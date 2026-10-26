using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using BatihanDev.UnityCliCommands.Profiler.Internal;
using BatihanDev.UnityCliCommands.Profiler.Schemas;

namespace BatihanDev.UnityCliCommands.Profiler.Analysis
{
    public static class ProfilerAnalysis
    {
        private static readonly IComparer<IReadOnlyList<string>> PathComparer =
            Comparer<IReadOnlyList<string>>.Create(ComparePaths);
        private static readonly IComparer<IReadOnlyList<IReadOnlyList<string>>> PathSetComparer =
            Comparer<IReadOnlyList<IReadOnlyList<string>>>.Create(ComparePathSets);

        public static CommandResult<TimeRangeResult> SummarizeTimeRange(
            IReadOnlyList<ProfilerFrameRecord> frames, int first, int last, double targetMs, string sessionId)
        {
            if (double.IsNaN(targetMs) || double.IsInfinity(targetMs) || targetMs <= 0d)
                return Fail<TimeRangeResult>(ProfilerSchema.TimeRange, ProfilerErrorCode.TargetMsInvalid);
            if (first > last)
                return Fail<TimeRangeResult>(ProfilerSchema.TimeRange, ProfilerErrorCode.FrameRangeInvalid);
            var selected = frames.Where(frame => frame.FrameIndex >= first && frame.FrameIndex <= last)
                .OrderBy(frame => frame.FrameIndex).ToList();
            if (selected.Count == 0 || selected[0].FrameIndex != first || selected[selected.Count - 1].FrameIndex != last)
                return Fail<TimeRangeResult>(ProfilerSchema.TimeRange, ProfilerErrorCode.FrameRangeInvalid);
            for (var index = 1; index < selected.Count; index++)
                if (selected[index].FrameIndex != selected[index - 1].FrameIndex + 1)
                    return Fail<TimeRangeResult>(ProfilerSchema.TimeRange, ProfilerErrorCode.FrameRangeGap);

            var ordered = selected.Select(frame => frame.FrameTimeMs).OrderBy(value => value).ToArray();
            var median = ordered.Length % 2 == 1
                ? ordered[ordered.Length / 2]
                : (ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) / 2d;
            var maximum = ordered[ordered.Length - 1];
            var over = selected.Count(frame => frame.FrameTimeMs > targetMs);
            var maxFrame = selected.Where(frame => frame.FrameTimeMs == maximum).Min(frame => frame.FrameIndex);
            var overBudgetPercent = 100d * over / selected.Count;
            var maxOverBudgetMs = Math.Max(0d, maximum - targetMs);
            var maxBudgetRatio = maximum / targetMs;
            if (!IsFinite(overBudgetPercent) || !IsFinite(maxOverBudgetMs) || !IsFinite(maxBudgetRatio))
                return CommandResult<TimeRangeResult>.Failure(ProfilerSchema.TimeRange,
                    ProfilerErrorCode.ProfilerDataUnavailable,
                    "Profiler budget arithmetic is outside the finite numeric result range.");
            return CommandResult<TimeRangeResult>.Success(ProfilerSchema.TimeRange,
                new TimeRangeResult(sessionId, first, last, targetMs, selected.Count, ordered[0], median,
                    ordered[(int)Math.Ceiling(0.95d * ordered.Length) - 1], maximum, maxFrame, over,
                    overBudgetPercent, maxOverBudgetMs, maxBudgetRatio));
        }

        public static CommandResult<FrameSelfResult> FrameSelf(ProfilerFrameRecord frame, int? threadIndex,
            string threadName, int limit, long epoch, string sessionId)
        {
            var selected = SelectThread(frame, threadIndex, threadName);
            if (!selected.Ok)
                return CommandResult<FrameSelfResult>.Failure(ProfilerSchema.FrameSelf, selected.Error);
            var thread = selected.Result;
            var bottomUpRoot = thread.BottomUpSamples.FirstOrDefault(sample => !sample.ParentItemId.HasValue);
            var sourceRows = ReferenceEquals(thread.BottomUpSamples, thread.Samples)
                ? thread.BottomUpSamples.Where(sample => sample.SelfTimeNs > 0 &&
                    !thread.BottomUpSamples.Any(child => child.ParentItemId == sample.ItemId))
                : thread.BottomUpSamples.Where(sample => sample.SelfTimeNs > 0 && bottomUpRoot != null &&
                    sample.ParentItemId == bottomUpRoot.ItemId);
            var rows = new List<SelfTimeRow>();
            foreach (var sample in sourceRows)
            {
                var paths = ResolveBottomUpPaths(thread, sample);
                if (paths.Count == 0)
                    return Fail<FrameSelfResult>(ProfilerSchema.FrameSelf,
                        ProfilerErrorCode.ProfilerDataUnavailable);
                rows.Add(new SelfTimeRow(
                    ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, epoch, ProfilerViewKind.BottomUp,
                        frame.FrameIndex, thread.ThreadIndex, sample.ItemId)),
                    paths, sample.TotalMs, sample.SelfMs, sample.Calls));
            }
            rows = rows
                .OrderByDescending(row => row.SelfMs)
                .ThenBy(row => row.MarkerPaths, PathSetComparer)
                .ThenBy(row => row.BottomUpId, StringComparer.Ordinal).ToList();
            var available = rows.Count;
            return CommandResult<FrameSelfResult>.Success(ProfilerSchema.FrameSelf,
                new FrameSelfResult(sessionId, frame.FrameIndex, Identity(thread),
                    rows.Take(Math.Max(0, limit)).ToList(), available));
        }

        public static CommandResult<SampleTimeResult> Sample(ProfilerFrameRecord frame, ProfilerThreadRecord thread,
            string sampleId, long epoch, string sessionId)
        {
            var decoded = ProfilerOpaqueId.Decode(sampleId, epoch, ProfilerViewKind.Normal);
            if (!decoded.Ok)
                return CommandResult<SampleTimeResult>.Failure(ProfilerSchema.SampleTime, decoded.Error);
            if (decoded.Result.FrameIndex != frame.FrameIndex || decoded.Result.ThreadIndex != thread.ThreadIndex)
                return Fail<SampleTimeResult>(ProfilerSchema.SampleTime, ProfilerErrorCode.SampleIdInvalid);
            var sample = thread.Samples.FirstOrDefault(value => value.ItemId == decoded.Result.ItemId);
            if (sample == null)
                return Fail<SampleTimeResult>(ProfilerSchema.SampleTime, ProfilerErrorCode.SampleIdInvalid);
            var ancestors = Ancestors(thread, sample).Select(value => Summary(thread, value, epoch,
                frame.FrameIndex, ProfilerViewKind.Normal)).ToList();
            var children = thread.Samples.Where(value => value.ParentItemId == sample.ItemId)
                .Select(value => Summary(thread, value, epoch, frame.FrameIndex, ProfilerViewKind.Normal))
                .OrderByDescending(value => value.TotalMs)
                .ThenBy(value => value.MarkerPath, PathComparer).ToList();
            var selectedRawIndices = new HashSet<int>(sample.RawSampleIndices);
            var occurrences = thread.RawSamples.Where(value => selectedRawIndices.Contains(value.ItemId))
                .Select(value => Summary(thread, value, epoch, frame.FrameIndex, ProfilerViewKind.Raw))
                .OrderByDescending(value => value.TotalMs)
                .ThenBy(value => value.MarkerPath, PathComparer)
                .ThenBy(value => value.SampleId, StringComparer.Ordinal)
                .ToList();
            return CommandResult<SampleTimeResult>.Success(ProfilerSchema.SampleTime,
                new SampleTimeResult(sessionId, frame.FrameIndex, Identity(thread), sampleId, sample.MarkerName,
                    BuildPath(thread, sample), sample.TotalMs, sample.SelfMs, sample.Calls,
                    children.Sum(value => value.TotalMs), ancestors, children, occurrences));
        }

        public static CommandResult<BottomUpResult> BottomUp(ProfilerFrameRecord frame, ProfilerThreadRecord thread,
            string bottomUpId, long epoch, string sessionId)
        {
            var decoded = ProfilerOpaqueId.Decode(bottomUpId, epoch, ProfilerViewKind.BottomUp);
            if (!decoded.Ok)
                return CommandResult<BottomUpResult>.Failure(ProfilerSchema.BottomUp, decoded.Error);
            if (decoded.Result.FrameIndex != frame.FrameIndex || decoded.Result.ThreadIndex != thread.ThreadIndex)
                return Fail<BottomUpResult>(ProfilerSchema.BottomUp, ProfilerErrorCode.SampleIdInvalid);
            var sample = thread.BottomUpSamples.FirstOrDefault(value => value.ItemId == decoded.Result.ItemId);
            if (sample == null)
                return Fail<BottomUpResult>(ProfilerSchema.BottomUp, ProfilerErrorCode.ProfilerDataUnavailable);
            var markerPaths = ResolveBottomUpPaths(thread, sample);
            if (markerPaths.Count == 0)
                return Fail<BottomUpResult>(ProfilerSchema.BottomUp, ProfilerErrorCode.ProfilerDataUnavailable);
            var callers = thread.BottomUpSamples.Where(value => value.ParentItemId == sample.ItemId)
                .Select(value => AggregateSummary(thread, value, epoch, frame.FrameIndex))
                .ToList();
            if (callers.Any(value => value == null))
                return Fail<BottomUpResult>(ProfilerSchema.BottomUp, ProfilerErrorCode.ProfilerDataUnavailable);
            var typedCallers = callers.Cast<SelfTimeRow>().ToList();
            typedCallers = typedCallers.OrderByDescending(value => value.TotalMs)
                .ThenBy(value => value.MarkerPaths, PathSetComparer)
                .ThenBy(value => value.BottomUpId, StringComparer.Ordinal).ToList();
            var aggregateRawIndices = new HashSet<int>(sample.RawSampleIndices);
            var occurrences = thread.RawSamples.Where(value => aggregateRawIndices.Count > 0
                    ? aggregateRawIndices.Contains(value.ItemId)
                    : value.MarkerId == sample.MarkerId)
                .Select(value => Summary(thread, value, epoch, frame.FrameIndex, ProfilerViewKind.Raw))
                .OrderByDescending(value => value.TotalMs)
                .ThenBy(value => value.MarkerPath, PathComparer)
                .ThenBy(value => value.SampleId, StringComparer.Ordinal)
                .ToList();
            if (occurrences.Count == 0)
            {
                return Fail<BottomUpResult>(ProfilerSchema.BottomUp,
                    ProfilerErrorCode.ProfilerDataUnavailable);
            }
            return CommandResult<BottomUpResult>.Success(ProfilerSchema.BottomUp,
                new BottomUpResult(sessionId, frame.FrameIndex, Identity(thread), bottomUpId, sample.MarkerName,
                    markerPaths, sample.TotalMs, sample.SelfMs, sample.Calls,
                    occurrences.Sum(value => value.TotalMs), typedCallers, occurrences));
        }

        public static CommandResult<MarkerTimeResult> MarkerPath(ProfilerFrameRecord frame,
            ProfilerThreadRecord thread, IReadOnlyList<string> markerPath, long epoch, string sessionId)
        {
            var found = FindMarkerPath(thread, markerPath);
            if (!found.Ok)
                return CommandResult<MarkerTimeResult>.Failure(ProfilerSchema.MarkerTime, found.Error);
            var sample = found.Result;
            var sampleId = ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, epoch, ProfilerViewKind.Normal,
                frame.FrameIndex, thread.ThreadIndex, sample.ItemId));
            return CommandResult<MarkerTimeResult>.Success(ProfilerSchema.MarkerTime,
                new MarkerTimeResult(sessionId, frame.FrameIndex, Identity(thread), sampleId,
                    BuildPath(thread, sample), sample.TotalMs, sample.SelfMs, sample.Calls));
        }

        public static CommandResult<ProfilerThreadRecord> SelectThread(
            ProfilerFrameRecord frame, int? threadIndex, string threadName)
        {
            if (threadIndex.HasValue && threadName != null)
                return Fail<ProfilerThreadRecord>(ProfilerSchema.FrameTotal, ProfilerErrorCode.ThreadSelectorConflict);
            var matches = threadIndex.HasValue
                ? frame.Threads.Where(thread => thread.ThreadIndex == threadIndex.Value).ToList()
                : frame.Threads.Where(thread => string.Equals(thread.ThreadName, threadName ?? "Main Thread",
                    StringComparison.Ordinal)).ToList();
            if (matches.Count == 0)
                return Fail<ProfilerThreadRecord>(ProfilerSchema.FrameTotal, ProfilerErrorCode.ThreadNotFound);
            if (matches.Count > 1)
                return Fail<ProfilerThreadRecord>(ProfilerSchema.FrameTotal, ProfilerErrorCode.ThreadAmbiguous);
            return CommandResult<ProfilerThreadRecord>.Success(ProfilerSchema.FrameTotal, matches[0]);
        }

        public static CommandResult<FrameTotalResult> FrameTotal(ProfilerFrameRecord frame, double targetMs,
            int? threadIndex, string threadName, int limit, long epoch, string sessionId)
        {
            if (double.IsNaN(targetMs) || double.IsInfinity(targetMs) || targetMs <= 0d)
                return Fail<FrameTotalResult>(ProfilerSchema.FrameTotal, ProfilerErrorCode.TargetMsInvalid);
            var selected = SelectThread(frame, threadIndex, threadName);
            if (!selected.Ok) return CommandResult<FrameTotalResult>.Failure(ProfilerSchema.FrameTotal, selected.Error);
            var thread = selected.Result;
            var budgetRatio = frame.FrameTimeMs / targetMs;
            var overBudgetMs = Math.Max(0d, frame.FrameTimeMs - targetMs);
            if (!IsFinite(budgetRatio) || !IsFinite(overBudgetMs))
                return UnavailableFrameTotal();

            var rows = new List<TimeRow>();
            foreach (var sample in thread.Samples)
            {
                var percentOfFrame = frame.FrameTimeNs > 0
                    ? 100d * sample.TotalMs / frame.FrameTimeMs
                    : (double?)null;
                var percentOfBudget = 100d * sample.TotalMs / targetMs;
                if ((percentOfFrame.HasValue && !IsFinite(percentOfFrame.Value)) || !IsFinite(percentOfBudget))
                    return UnavailableFrameTotal();
                rows.Add(new TimeRow(
                    ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, epoch, ProfilerViewKind.Normal,
                        frame.FrameIndex, thread.ThreadIndex, sample.ItemId)),
                    BuildPath(thread, sample), sample.TotalMs, sample.SelfMs, sample.Calls,
                    percentOfFrame, percentOfBudget));
            }
            rows = rows
                .OrderByDescending(row => row.TotalMs)
                .ThenBy(row => row.MarkerPath, PathComparer)
                .ThenBy(row => row.SampleId, StringComparer.Ordinal).ToList();
            var available = rows.Count;
            rows = rows.Take(Math.Max(0, limit)).ToList();
            return CommandResult<FrameTotalResult>.Success(ProfilerSchema.FrameTotal,
                new FrameTotalResult(sessionId, frame.FrameIndex, targetMs, frame.FrameTimeMs,
                    Identity(thread), rows, available));
        }

        private static CommandResult<FrameTotalResult> UnavailableFrameTotal()
        {
            return CommandResult<FrameTotalResult>.Failure(
                ProfilerSchema.FrameTotal,
                ProfilerErrorCode.ProfilerDataUnavailable,
                "Profiler budget arithmetic is outside the finite numeric result range.");
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static CommandResult<IReadOnlyList<ProfilerFrameRecord>> SelectContiguousRange(
            IReadOnlyList<ProfilerFrameRecord> frames, int first, int last, string schema)
        {
            if (first > last)
                return Fail<IReadOnlyList<ProfilerFrameRecord>>(schema, ProfilerErrorCode.FrameRangeInvalid);
            var selected = frames.Where(frame => frame.FrameIndex >= first && frame.FrameIndex <= last)
                .OrderBy(frame => frame.FrameIndex).ToList();
            if (selected.Count == 0 || selected[0].FrameIndex != first ||
                selected[selected.Count - 1].FrameIndex != last)
                return Fail<IReadOnlyList<ProfilerFrameRecord>>(schema, ProfilerErrorCode.FrameRangeInvalid);
            for (var index = 1; index < selected.Count; index++)
                if (selected[index].FrameIndex != selected[index - 1].FrameIndex + 1)
                    return Fail<IReadOnlyList<ProfilerFrameRecord>>(schema, ProfilerErrorCode.FrameRangeGap);
            return CommandResult<IReadOnlyList<ProfilerFrameRecord>>.Success(schema, selected);
        }

        private static CommandResult<IReadOnlyList<AllocationSite>> AttributeAllocations(
            ProfilerFrameRecord frame, ProfilerThreadRecord thread, string schema)
        {
            if (!thread.AllocationDataAvailable)
                return Fail<IReadOnlyList<AllocationSite>>(schema, ProfilerErrorCode.AllocationDataUnavailable);
            try
            {
                var rawById = thread.RawSamples.ToDictionary(value => value.ItemId);
                var groups = new Dictionary<int, AllocationSite>();
                foreach (var allocation in thread.RawSamples.Where(IsAllocationEvent))
                {
                    if (allocation.AllocationBytes < 0 || allocation.AllocationCount <= 0)
                        return UnavailableGc<IReadOnlyList<AllocationSite>>(schema);
                    var site = FindAllocationSite(thread, rawById, allocation);
                    if (site == null)
                        return UnavailableGc<IReadOnlyList<AllocationSite>>(schema);
                    if (!groups.TryGetValue(site.ItemId, out var group))
                    {
                        group = new AllocationSite(frame, thread, site, BuildPath(thread, site), 0, 0);
                        groups.Add(site.ItemId, group);
                    }
                    group.Add(allocation);
                }
                return CommandResult<IReadOnlyList<AllocationSite>>.Success(schema,
                    groups.Values.OrderBy(value => value.MarkerPath, PathComparer)
                        .ThenBy(value => value.NormalSample.ItemId).ToList());
            }
            catch (OverflowException)
            {
                return UnavailableGc<IReadOnlyList<AllocationSite>>(schema);
            }
        }

        private static bool IsAllocationEvent(ProfilerSampleRecord sample)
        {
            return string.Equals(sample.MarkerName, "GC.Alloc", StringComparison.Ordinal) &&
                (sample.AllocationCount != 0 || sample.AllocationBytes != 0);
        }

        private static ProfilerSampleRecord FindAllocationSite(ProfilerThreadRecord thread,
            IReadOnlyDictionary<int, ProfilerSampleRecord> rawById, ProfilerSampleRecord allocation)
        {
            var parentId = allocation.ParentItemId;
            while (parentId.HasValue && rawById.TryGetValue(parentId.Value, out var parent))
            {
                if (!string.Equals(parent.MarkerName, "GC.Alloc", StringComparison.Ordinal))
                {
                    var normalSite = FindIndexedNormalMatch(thread, parent, out var ambiguous);
                    if (ambiguous)
                        return null;
                    if (normalSite != null)
                        return normalSite;
                }
                parentId = parent.ParentItemId;
            }
            return FindIndexedNormalMatch(thread, allocation, out _);
        }

        private static ProfilerSampleRecord FindIndexedNormalMatch(ProfilerThreadRecord thread,
            ProfilerSampleRecord rawSample, out bool ambiguous)
        {
            ambiguous = false;
            if (rawSample == null)
                return null;
            var indexedMatches = thread.Samples.Where(value => value.RawSampleIndices.Contains(rawSample.ItemId))
                .ToList();
            if (indexedMatches.Count == 1)
                return indexedMatches[0];
            ambiguous = indexedMatches.Count > 1;
            return null;
        }

        private static CommandResult<GcAttributionResult> AttributeSample(ProfilerFrameRecord frame,
            ProfilerThreadRecord thread, ProfilerSampleRecord sample, string sampleId, long epoch,
            string sessionId, string schema)
        {
            var attributed = AttributeAllocations(frame, thread, schema);
            if (!attributed.Ok)
                return CommandResult<GcAttributionResult>.Failure(schema, attributed.Error);
            try
            {
                var path = BuildPath(thread, sample);
                if (sample.RawSampleIndices.Count == 0)
                    return UnavailableGc<GcAttributionResult>(schema);
                var rawById = thread.RawSamples.ToDictionary(value => value.ItemId);
                var selectedRoots = new HashSet<int>(sample.RawSampleIndices);
                var inclusive = attributed.Result.SelectMany(site => site.Contributions)
                    .Where(value => IsInRawSubtree(value.RawSampleId, selectedRoots, rawById)).ToList();
                var direct = attributed.Result.Where(site => site.NormalSample.ItemId == sample.ItemId).ToList();
                return CommandResult<GcAttributionResult>.Success(schema,
                    new GcAttributionResult(sessionId, frame.FrameIndex, Identity(thread), sampleId, path,
                        CheckedSum(inclusive.Select(value => value.Bytes)),
                        CheckedSum(inclusive.Select(value => value.Count)),
                        SumBytes(direct), SumCounts(direct)));
            }
            catch (OverflowException)
            {
                return UnavailableGc<GcAttributionResult>(schema);
            }
        }

        private static bool IsInRawSubtree(int rawSampleId, HashSet<int> selectedRoots,
            IReadOnlyDictionary<int, ProfilerSampleRecord> rawById)
        {
            var currentId = (int?)rawSampleId;
            var visited = new HashSet<int>();
            while (currentId.HasValue && visited.Add(currentId.Value))
            {
                if (selectedRoots.Contains(currentId.Value))
                    return true;
                currentId = rawById.TryGetValue(currentId.Value, out var current)
                    ? current.ParentItemId : null;
            }
            return false;
        }

        private static IReadOnlyList<GcAggregateRow> CombineAllocationPaths(
            IReadOnlyList<AllocationSite> sites)
        {
            var groups = new List<GcAggregateRow>();
            foreach (var site in sites)
            {
                var existingIndex = groups.FindIndex(value => PathsEqual(value.MarkerPath, site.MarkerPath));
                if (existingIndex < 0)
                {
                    groups.Add(new GcAggregateRow(site.MarkerPath, site.Bytes, site.Count));
                    continue;
                }
                var existing = groups[existingIndex];
                groups[existingIndex] = new GcAggregateRow(existing.MarkerPath,
                    checked(existing.Bytes + site.Bytes), checked(existing.Count + site.Count));
            }
            return groups.OrderByDescending(value => value.Bytes)
                .ThenBy(value => value.MarkerPath, PathComparer).ToList();
        }

        private static long SumBytes(IEnumerable<AllocationSite> sites)
        {
            return CheckedSum(sites.Select(value => value.Bytes));
        }

        private static long SumCounts(IEnumerable<AllocationSite> sites)
        {
            return CheckedSum(sites.Select(value => value.Count));
        }

        private static long CheckedSum(IEnumerable<long> values)
        {
            long total = 0;
            foreach (var value in values)
                total = checked(total + value);
            return total;
        }

        private static CommandResult<T> UnavailableGc<T>(string schema)
        {
            return CommandResult<T>.Failure(schema, ProfilerErrorCode.ProfilerDataUnavailable,
                "Profiler allocation arithmetic or attribution is unavailable without loss.");
        }

        public static CommandResult<ProfilerSampleRecord> FindMarkerPath(
            ProfilerThreadRecord thread, IReadOnlyList<string> markerPath)
        {
            var matches = thread.Samples.Where(sample => PathsEqual(BuildPath(thread, sample), markerPath)).ToList();
            if (matches.Count == 0)
                return Fail<ProfilerSampleRecord>(ProfilerSchema.FrameTotal, ProfilerErrorCode.MarkerPathNotFound);
            if (matches.Count > 1)
                return Fail<ProfilerSampleRecord>(ProfilerSchema.FrameTotal, ProfilerErrorCode.MarkerPathAmbiguous);
            return CommandResult<ProfilerSampleRecord>.Success(ProfilerSchema.FrameTotal, matches[0]);
        }

        public static CommandResult<GcFrameResult> FrameGc(ProfilerFrameRecord frame, int? threadIndex,
            string threadName, long thresholdBytes, long epoch, string sessionId)
        {
            if (thresholdBytes < 0)
                return Fail<GcFrameResult>(ProfilerSchema.GcFrame, ProfilerErrorCode.ThresholdBytesInvalid);
            var selected = SelectThread(frame, threadIndex, threadName);
            if (!selected.Ok) return CommandResult<GcFrameResult>.Failure(ProfilerSchema.GcFrame, selected.Error);
            var thread = selected.Result;
            var attributed = AttributeAllocations(frame, thread, ProfilerSchema.GcFrame);
            if (!attributed.Ok)
                return CommandResult<GcFrameResult>.Failure(ProfilerSchema.GcFrame, attributed.Error);
            try
            {
                var sites = attributed.Result;
                var reported = sites.Where(site => site.Bytes >= thresholdBytes).ToList();
                var rows = reported.Select(site => new GcSiteRow(
                        ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, epoch, ProfilerViewKind.Normal,
                            frame.FrameIndex, thread.ThreadIndex, site.NormalSample.ItemId)),
                        site.MarkerPath, site.Bytes, site.Count))
                    .OrderByDescending(row => row.Bytes)
                    .ThenBy(row => row.MarkerPath, PathComparer)
                    .ThenBy(row => row.SampleId, StringComparer.Ordinal).ToList();
                return CommandResult<GcFrameResult>.Success(ProfilerSchema.GcFrame,
                    new GcFrameResult(sessionId, frame.FrameIndex, Identity(thread), thresholdBytes,
                        SumBytes(sites), SumCounts(sites), SumBytes(reported), SumCounts(reported), rows));
            }
            catch (OverflowException)
            {
                return UnavailableGc<GcFrameResult>(ProfilerSchema.GcFrame);
            }
        }

        public static CommandResult<GcOverallResult> OverallGc(IReadOnlyList<ProfilerFrameRecord> frames,
            long thresholdBytes, long epoch, string sessionId)
        {
            if (thresholdBytes < 0)
                return Fail<GcOverallResult>(ProfilerSchema.GcOverall, ProfilerErrorCode.ThresholdBytesInvalid);
            try
            {
                var allSites = new List<AllocationSite>();
                var reportedSites = new List<AllocationSite>();
                foreach (var frame in frames.OrderBy(value => value.FrameIndex))
                foreach (var thread in frame.Threads.OrderBy(value => value.ThreadIndex))
                {
                    var attributed = AttributeAllocations(frame, thread, ProfilerSchema.GcOverall);
                    if (!attributed.Ok)
                        return CommandResult<GcOverallResult>.Failure(ProfilerSchema.GcOverall, attributed.Error);
                    allSites.AddRange(attributed.Result);
                    reportedSites.AddRange(attributed.Result.Where(site => site.Bytes >= thresholdBytes));
                }

                var rows = CombineAllocationPaths(reportedSites);
                return CommandResult<GcOverallResult>.Success(ProfilerSchema.GcOverall,
                    new GcOverallResult(sessionId, frames.Count, thresholdBytes, SumBytes(allSites),
                        SumCounts(allSites), SumBytes(reportedSites), SumCounts(reportedSites), rows));
            }
            catch (OverflowException)
            {
                return UnavailableGc<GcOverallResult>(ProfilerSchema.GcOverall);
            }
        }

        public static CommandResult<GcRangeResult> RangeGc(IReadOnlyList<ProfilerFrameRecord> frames,
            int first, int last, long thresholdBytes, string sessionId)
        {
            if (thresholdBytes < 0)
                return Fail<GcRangeResult>(ProfilerSchema.GcRange, ProfilerErrorCode.ThresholdBytesInvalid);
            var selected = SelectContiguousRange(frames, first, last, ProfilerSchema.GcRange);
            if (!selected.Ok)
                return CommandResult<GcRangeResult>.Failure(ProfilerSchema.GcRange, selected.Error);
            try
            {
                var summaries = new List<GcFrameSummary>();
                foreach (var frame in selected.Result)
                {
                    var sites = new List<AllocationSite>();
                    foreach (var thread in frame.Threads.OrderBy(value => value.ThreadIndex))
                    {
                        var attributed = AttributeAllocations(frame, thread, ProfilerSchema.GcRange);
                        if (!attributed.Ok)
                            return CommandResult<GcRangeResult>.Failure(ProfilerSchema.GcRange, attributed.Error);
                        sites.AddRange(attributed.Result);
                    }
                    var reported = sites.Where(site => site.Bytes >= thresholdBytes).ToList();
                    summaries.Add(new GcFrameSummary(frame.FrameIndex, SumBytes(sites), SumCounts(sites),
                        SumBytes(reported), SumCounts(reported)));
                }
                var ordered = summaries.Select(value => value.TotalBytes).OrderBy(value => value).ToArray();
                var maximum = ordered[ordered.Length - 1];
                var median = ordered.Length % 2 == 1
                    ? ordered[ordered.Length / 2]
                    : ((decimal)ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) / 2m;
                return CommandResult<GcRangeResult>.Success(ProfilerSchema.GcRange,
                    new GcRangeResult(sessionId, first, last, summaries.Count, thresholdBytes,
                        CheckedSum(summaries.Select(value => value.TotalBytes)),
                        CheckedSum(summaries.Select(value => value.TotalCount)),
                        CheckedSum(summaries.Select(value => value.ReportedBytes)),
                        CheckedSum(summaries.Select(value => value.ReportedCount)),
                        ordered[0], median, ordered[(int)Math.Ceiling(0.95d * ordered.Length) - 1], maximum,
                        summaries.Where(value => value.TotalBytes == maximum).Min(value => value.FrameIndex),
                        summaries));
            }
            catch (OverflowException)
            {
                return UnavailableGc<GcRangeResult>(ProfilerSchema.GcRange);
            }
        }

        public static CommandResult<GcAttributionResult> SampleGc(ProfilerFrameRecord frame,
            ProfilerThreadRecord thread, string sampleId, long epoch, string sessionId)
        {
            var decoded = ProfilerOpaqueId.Decode(sampleId, epoch, ProfilerViewKind.Normal);
            if (!decoded.Ok)
                return CommandResult<GcAttributionResult>.Failure(ProfilerSchema.GcSample, decoded.Error);
            if (decoded.Result.FrameIndex != frame.FrameIndex || decoded.Result.ThreadIndex != thread.ThreadIndex)
                return Fail<GcAttributionResult>(ProfilerSchema.GcSample, ProfilerErrorCode.SampleIdInvalid);
            var sample = thread.Samples.FirstOrDefault(value => value.ItemId == decoded.Result.ItemId);
            if (sample == null)
                return Fail<GcAttributionResult>(ProfilerSchema.GcSample, ProfilerErrorCode.SampleIdInvalid);
            return AttributeSample(frame, thread, sample, sampleId, epoch, sessionId, ProfilerSchema.GcSample);
        }

        public static CommandResult<GcAttributionResult> MarkerGc(ProfilerFrameRecord frame,
            ProfilerThreadRecord thread, IReadOnlyList<string> markerPath, long epoch, string sessionId)
        {
            var found = FindMarkerPath(thread, markerPath);
            if (!found.Ok)
                return CommandResult<GcAttributionResult>.Failure(ProfilerSchema.GcMarker, found.Error);
            var sampleId = ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, epoch, ProfilerViewKind.Normal,
                frame.FrameIndex, thread.ThreadIndex, found.Result.ItemId));
            return AttributeSample(frame, thread, found.Result, sampleId, epoch, sessionId, ProfilerSchema.GcMarker);
        }

        public static CommandResult<RelatedTimeResult> Related(ProfilerFrameRecord frame,
            ProfilerThreadRecord sourceThread, string sourceSampleId, ProfilerThreadRecord targetThread,
            long epoch, string sessionId)
        {
            var decoded = ProfilerOpaqueId.Decode(sourceSampleId, epoch, ProfilerViewKind.Raw);
            if (!decoded.Ok)
                return CommandResult<RelatedTimeResult>.Failure(ProfilerSchema.RelatedTime, decoded.Error);
            if (decoded.Result.FrameIndex != frame.FrameIndex || decoded.Result.ThreadIndex != sourceThread.ThreadIndex)
                return Fail<RelatedTimeResult>(ProfilerSchema.RelatedTime, ProfilerErrorCode.SampleIdInvalid);
            var source = sourceThread.RawSamples.FirstOrDefault(value => value.ItemId == decoded.Result.ItemId);
            if (source == null)
                return Fail<RelatedTimeResult>(ProfilerSchema.RelatedTime, ProfilerErrorCode.SampleIdInvalid);
            long sourceEnd;
            try
            {
                sourceEnd = checked(source.StartTimeNs + source.TotalTimeNs);
            }
            catch (OverflowException)
            {
                return UnavailableRelated();
            }
            var overlaps = new List<RelatedRow>();
            foreach (var sample in targetThread.RawSamples.Where(value => !string.IsNullOrEmpty(value.MarkerName)))
            {
                long sampleEnd;
                try
                {
                    sampleEnd = checked(sample.StartTimeNs + sample.TotalTimeNs);
                }
                catch (OverflowException)
                {
                    return UnavailableRelated();
                }
                var start = Math.Max(source.StartTimeNs, sample.StartTimeNs);
                var end = Math.Min(sourceEnd, sampleEnd);
                if (end <= start)
                    continue;
                overlaps.Add(new RelatedRow(
                    ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, epoch, ProfilerViewKind.Raw,
                        frame.FrameIndex, targetThread.ThreadIndex, sample.ItemId)),
                    BuildPath(targetThread.RawSamples, sample),
                    (end - start) / 1000000d, start / 1000000d,
                    SharesFlow(source, sample) ? "flow" : "temporal-overlap"));
            }
            var rows = overlaps
                .OrderByDescending(row => row.OverlapMs).ThenBy(row => row.StartMs)
                .ThenBy(row => row.MarkerPath, PathComparer)
                .ThenBy(row => row.SampleId, StringComparer.Ordinal).ToList();
            return CommandResult<RelatedTimeResult>.Success(ProfilerSchema.RelatedTime,
                new RelatedTimeResult(sessionId, frame.FrameIndex, sourceSampleId, Identity(sourceThread),
                    Identity(targetThread), rows));
        }

        private static CommandResult<RelatedTimeResult> UnavailableRelated()
        {
            return CommandResult<RelatedTimeResult>.Failure(ProfilerSchema.RelatedTime,
                ProfilerErrorCode.ProfilerDataUnavailable,
                "Profiler timestamp arithmetic is outside the supported range.");
        }

        public static CommandResult<ProfilerInvestigationResult> ComposeInvestigation(
            CommandResult<FrameSelfResult> leafSource, CommandResult<BottomUpResult> bottomUpSource,
            CommandResult<MarkerTimeResult> markerSource, IReadOnlyList<string> markerPath,
            double absoluteToleranceMs, double relativeTolerancePercent)
        {
            if (leafSource == null || !leafSource.Ok)
                return InvestigationSourceFailure("leaf", "profiler.time.frame-self", leafSource?.Error);
            if (!string.Equals(leafSource.Schema, ProfilerSchema.FrameSelf, StringComparison.Ordinal))
                return InvestigationReconciliationFailure(0d, 0d, 0d, 0d);

            var matchingLeaves = leafSource.Result.Rows
                .Where(row => row.MarkerPaths.Any(path => PathsEqual(path, markerPath))).ToList();
            if (matchingLeaves.Count != 1)
            {
                var code = matchingLeaves.Count == 0
                    ? ProfilerErrorCode.MarkerPathNotFound
                    : ProfilerErrorCode.MarkerPathAmbiguous;
                return InvestigationSourceFailure("leaf", "profiler.time.frame-self",
                    new CommandError { Code = code });
            }
            if (matchingLeaves[0].MarkerPaths.Count != 1)
                return InvestigationSourceFailure("leaf", "profiler.time.frame-self",
                    new CommandError
                    {
                        Code = ProfilerErrorCode.MarkerPathAmbiguous,
                        Details = new Dictionary<string, object>
                        {
                            { "candidates", matchingLeaves[0].MarkerPaths }
                        }
                    });
            if (bottomUpSource == null || !bottomUpSource.Ok)
                return InvestigationSourceFailure("bottomUp", "profiler.time.bottom-up", bottomUpSource?.Error);
            if (!string.Equals(bottomUpSource.Schema, ProfilerSchema.BottomUp, StringComparison.Ordinal))
                return InvestigationReconciliationFailure(0d, 0d, 0d, 0d);
            if (markerSource == null || !markerSource.Ok)
                return InvestigationSourceFailure("marker", "profiler.time.marker-path", markerSource?.Error);
            if (!string.Equals(markerSource.Schema, ProfilerSchema.MarkerTime, StringComparison.Ordinal))
                return InvestigationReconciliationFailure(0d, 0d, 0d, 0d);

            var leaf = matchingLeaves[0];
            var bottomUp = bottomUpSource.Result;
            var marker = markerSource.Result;
            if (!SameContext(leafSource.Result, bottomUp) || !SameContext(leafSource.Result, marker) ||
                leaf.BottomUpId != bottomUp.BottomUpId || !PathsEqual(marker.MarkerPath, markerPath) ||
                bottomUp.MarkerPaths == null || bottomUp.MarkerPaths.Count != 1 ||
                !PathsEqual(bottomUp.MarkerPaths[0], markerPath) ||
                string.IsNullOrEmpty(leaf.BottomUpId) || string.IsNullOrEmpty(marker.MarkerSampleId) ||
                !IsFinite(absoluteToleranceMs) || absoluteToleranceMs < 0d ||
                !IsFinite(relativeTolerancePercent) || relativeTolerancePercent < 0d ||
                !IsFinite(leaf.SelfMs) || !IsFinite(bottomUp.SelfMs) ||
                !IsFinite(bottomUp.OccurrenceTotalMs) || !IsFinite(marker.TotalMs))
                return InvestigationReconciliationFailure(0d, 0d, 0d, 0d);

            var leafAbsoluteDelta = Math.Abs(leaf.SelfMs - bottomUp.SelfMs);
            var occurrenceAbsoluteDelta = Math.Abs(bottomUp.OccurrenceTotalMs - marker.TotalMs);
            var leafRelativeDelta = RelativeDeltaPercent(leaf.SelfMs, bottomUp.SelfMs);
            var occurrenceRelativeDelta = RelativeDeltaPercent(bottomUp.OccurrenceTotalMs, marker.TotalMs);
            if (!IsFinite(leafAbsoluteDelta) || !IsFinite(occurrenceAbsoluteDelta) ||
                !IsFinite(leafRelativeDelta) || !IsFinite(occurrenceRelativeDelta))
                return InvestigationReconciliationFailure(leafAbsoluteDelta, leafRelativeDelta,
                    occurrenceAbsoluteDelta, occurrenceRelativeDelta);

            var leafMatches = leafAbsoluteDelta <= absoluteToleranceMs ||
                leafRelativeDelta <= relativeTolerancePercent;
            var callersMatch = occurrenceAbsoluteDelta <= absoluteToleranceMs ||
                occurrenceRelativeDelta <= relativeTolerancePercent;
            if (!leafMatches || !callersMatch)
                return InvestigationReconciliationFailure(leafAbsoluteDelta, leafRelativeDelta,
                    occurrenceAbsoluteDelta, occurrenceRelativeDelta);

            var reconciliation = new InvestigationReconciliation(absoluteToleranceMs,
                relativeTolerancePercent, leaf.SelfMs, bottomUp.SelfMs, bottomUp.OccurrenceTotalMs,
                marker.TotalMs, leafMatches, callersMatch);
            return CommandResult<ProfilerInvestigationResult>.Success(ProfilerSchema.Investigation,
                new ProfilerInvestigationResult(leafSource.Result.SessionId, leafSource.Result.FrameIndex,
                    leafSource.Result.Thread, markerPath,
                    new InvestigationLinkedIds(leaf.BottomUpId, marker.MarkerSampleId),
                    new InvestigationLeafSource(leafSource.Schema, leaf),
                    new InvestigationBottomUpSource(bottomUpSource.Schema, bottomUp),
                    new InvestigationMarkerSource(markerSource.Schema, marker), reconciliation));
        }

        private static CommandResult<ProfilerInvestigationResult> InvestigationSourceFailure(
            string step, string command, CommandError sourceError)
        {
            var sourceCode = sourceError?.Code ?? ProfilerErrorCode.ProfilerDataUnavailable;
            var details = new Dictionary<string, object>
            {
                { "step", step }, { "command", command }, { "sourceCode", sourceCode }
            };
            if (sourceError?.Details != null && sourceError.Details.TryGetValue("candidates", out var candidates))
                details["candidates"] = candidates;
            return CommandResult<ProfilerInvestigationResult>.Failure(ProfilerSchema.Investigation, sourceCode,
                "Profiler investigation source step failed.", details);
        }

        private static CommandResult<ProfilerInvestigationResult> InvestigationReconciliationFailure(
            double leafAbsoluteDeltaMs, double leafRelativeDeltaPercent,
            double occurrenceAbsoluteDeltaMs, double occurrenceRelativeDeltaPercent)
        {
            var details = new Dictionary<string, object> { { "step", "reconciliation" } };
            if (IsFinite(leafAbsoluteDeltaMs)) details["leafAbsoluteDeltaMs"] = leafAbsoluteDeltaMs;
            if (IsFinite(leafRelativeDeltaPercent)) details["leafRelativeDeltaPercent"] = leafRelativeDeltaPercent;
            if (IsFinite(occurrenceAbsoluteDeltaMs)) details["occurrenceAbsoluteDeltaMs"] = occurrenceAbsoluteDeltaMs;
            if (IsFinite(occurrenceRelativeDeltaPercent)) details["occurrenceRelativeDeltaPercent"] = occurrenceRelativeDeltaPercent;
            return CommandResult<ProfilerInvestigationResult>.Failure(ProfilerSchema.Investigation,
                ProfilerErrorCode.ProfilerDataUnavailable, "Profiler investigation reconciliation failed.", details);
        }

        private static bool SameContext(FrameSelfResult leaf, BottomUpResult bottomUp)
        {
            return leaf.SessionId == bottomUp.SessionId && leaf.FrameIndex == bottomUp.FrameIndex &&
                SameThread(leaf.Thread, bottomUp.Thread);
        }

        private static bool SameContext(FrameSelfResult leaf, MarkerTimeResult marker)
        {
            return leaf.SessionId == marker.SessionId && leaf.FrameIndex == marker.FrameIndex &&
                SameThread(leaf.Thread, marker.Thread);
        }

        private static bool SameThread(ThreadIdentity left, ThreadIdentity right)
        {
            return left != null && right != null && left.ThreadIndex == right.ThreadIndex &&
                left.ThreadId == right.ThreadId && left.ThreadGroupName == right.ThreadGroupName &&
                left.ThreadName == right.ThreadName;
        }

        private static double RelativeDeltaPercent(double left, double right)
        {
            var denominator = Math.Max(Math.Abs(left), Math.Abs(right));
            return denominator == 0d ? 0d : 100d * Math.Abs(left - right) / denominator;
        }

        private static SelfTimeRow AggregateSummary(ProfilerThreadRecord thread, ProfilerSampleRecord sample,
            long epoch, int frameIndex)
        {
            var paths = ResolveBottomUpPaths(thread, sample);
            return paths.Count == 0 ? null : new SelfTimeRow(
                ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, epoch, ProfilerViewKind.BottomUp,
                    frameIndex, thread.ThreadIndex, sample.ItemId)), paths,
                sample.TotalMs, sample.SelfMs, sample.Calls);
        }

        private static IReadOnlyList<IReadOnlyList<string>> ResolveBottomUpPaths(
            ProfilerThreadRecord thread, ProfilerSampleRecord sample)
        {
            if (sample.RawSampleIndices.Count > 0)
            {
                var rawByIndex = thread.RawSamples.ToDictionary(value => value.ItemId);
                var indexedPaths = new List<IReadOnlyList<string>>();
                foreach (var rawIndex in sample.RawSampleIndices)
                    if (rawByIndex.TryGetValue(rawIndex, out var rawSample))
                        AddDistinctPath(indexedPaths, BuildPath(thread.RawSamples, rawSample));
                indexedPaths.Sort(ComparePaths);
                return indexedPaths;
            }
            if (sample.MarkerId < 0)
                return Array.Empty<IReadOnlyList<string>>();
            var invertedChain = new List<int>();
            var byId = thread.BottomUpSamples.ToDictionary(value => value.ItemId);
            var current = sample;
            while (current != null && current.MarkerId >= 0 && current.ParentItemId.HasValue)
            {
                invertedChain.Add(current.MarkerId);
                current = current.ParentItemId.HasValue && byId.TryGetValue(current.ParentItemId.Value, out var parent)
                    ? parent : null;
            }
            invertedChain.Reverse();

            var paths = new List<IReadOnlyList<string>>();
            foreach (var rawTarget in thread.RawSamples.Where(value => value.MarkerId == invertedChain[0]))
            {
                var rawById = thread.RawSamples.ToDictionary(value => value.ItemId);
                var matched = rawTarget;
                var matchedAll = true;
                for (var index = 1; index < invertedChain.Count; index++)
                {
                    if (!matched.ParentItemId.HasValue ||
                        !rawById.TryGetValue(matched.ParentItemId.Value, out matched) ||
                        matched.MarkerId != invertedChain[index])
                    {
                        matchedAll = false;
                        break;
                    }
                }
                if (matchedAll)
                    AddDistinctPath(paths, BuildPath(thread.RawSamples, matched));
            }
            paths.Sort(ComparePaths);
            return paths;
        }

        private static IEnumerable<ProfilerSampleRecord> Ancestors(ProfilerThreadRecord thread,
            ProfilerSampleRecord sample)
        {
            var byId = thread.Samples.ToDictionary(value => value.ItemId);
            var result = new List<ProfilerSampleRecord>();
            var parentId = sample.ParentItemId;
            while (parentId.HasValue && byId.TryGetValue(parentId.Value, out var parent))
            {
                result.Add(parent);
                parentId = parent.ParentItemId;
            }
            result.Reverse();
            return result;
        }

        private static SampleSummary Summary(ProfilerThreadRecord thread, ProfilerSampleRecord sample, long epoch,
            int frameIndex, ProfilerViewKind kind)
        {
            return new SampleSummary(ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, epoch, kind,
                    frameIndex, thread.ThreadIndex, sample.ItemId)),
                kind == ProfilerViewKind.Raw ? BuildPath(thread.RawSamples, sample) : BuildPath(thread, sample),
                sample.TotalMs, sample.SelfMs, sample.Calls);
        }

        public static IReadOnlyList<string> BuildPath(ProfilerThreadRecord thread, ProfilerSampleRecord sample)
        {
            return BuildPath(thread.Samples, sample);
        }

        private static IReadOnlyList<string> BuildPath(IReadOnlyList<ProfilerSampleRecord> samples,
            ProfilerSampleRecord sample)
        {
            var byId = samples.ToDictionary(value => value.ItemId);
            var path = new List<string>();
            var current = sample;
            var visited = new HashSet<int>();
            while (current != null && visited.Add(current.ItemId))
            {
                if (!string.IsNullOrEmpty(current.MarkerName))
                    path.Add(current.MarkerName);
                current = current.ParentItemId.HasValue && byId.TryGetValue(current.ParentItemId.Value, out var parent)
                    ? parent : null;
            }
            path.Reverse();
            return path;
        }

        private static void AddDistinctPath(List<IReadOnlyList<string>> paths, IReadOnlyList<string> candidate)
        {
            if (!paths.Any(path => PathsEqual(path, candidate)))
                paths.Add(candidate);
        }

        private static int ComparePaths(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            var count = Math.Min(left.Count, right.Count);
            for (var index = 0; index < count; index++)
            {
                var comparison = string.Compare(left[index], right[index], StringComparison.Ordinal);
                if (comparison != 0)
                    return comparison;
            }
            return left.Count.CompareTo(right.Count);
        }

        private static int ComparePathSets(IReadOnlyList<IReadOnlyList<string>> left,
            IReadOnlyList<IReadOnlyList<string>> right)
        {
            var count = Math.Min(left.Count, right.Count);
            for (var index = 0; index < count; index++)
            {
                var comparison = ComparePaths(left[index], right[index]);
                if (comparison != 0)
                    return comparison;
            }
            return left.Count.CompareTo(right.Count);
        }

        private static bool SharesFlow(ProfilerSampleRecord left, ProfilerSampleRecord right)
        {
            return left.FlowIds.Intersect(right.FlowIds).Any();
        }

        private static bool PathsEqual(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            return left.Count == right.Count && left.SequenceEqual(right, StringComparer.Ordinal);
        }

        private static ThreadIdentity Identity(ProfilerThreadRecord thread)
        {
            return new ThreadIdentity(thread.ThreadIndex, thread.ThreadId, thread.ThreadGroupName, thread.ThreadName);
        }

        private static CommandResult<T> Fail<T>(string schema, string code)
        {
            return CommandResult<T>.Failure(schema, code, "Profiler data does not satisfy the requested operation.");
        }

        private sealed class AllocationSite
        {
            public AllocationSite(ProfilerFrameRecord frame, ProfilerThreadRecord thread,
                ProfilerSampleRecord normalSample, IReadOnlyList<string> markerPath, long bytes, long count)
            {
                Frame = frame; Thread = thread; NormalSample = normalSample; MarkerPath = markerPath;
                Bytes = bytes; Count = count;
            }
            public ProfilerFrameRecord Frame { get; }
            public ProfilerThreadRecord Thread { get; }
            public ProfilerSampleRecord NormalSample { get; }
            public IReadOnlyList<string> MarkerPath { get; }
            public long Bytes { get; set; }
            public long Count { get; set; }
            public IReadOnlyList<AllocationContribution> Contributions => _contributions;

            public void Add(ProfilerSampleRecord allocation)
            {
                Bytes = checked(Bytes + allocation.AllocationBytes);
                Count = checked(Count + allocation.AllocationCount);
                _contributions.Add(new AllocationContribution(allocation.ItemId,
                    allocation.AllocationBytes, allocation.AllocationCount));
            }

            private readonly List<AllocationContribution> _contributions = new List<AllocationContribution>();
        }

        private sealed class AllocationContribution
        {
            public AllocationContribution(int rawSampleId, long bytes, long count)
            {
                RawSampleId = rawSampleId;
                Bytes = bytes;
                Count = count;
            }

            public int RawSampleId { get; }
            public long Bytes { get; }
            public long Count { get; }
        }
    }
}
