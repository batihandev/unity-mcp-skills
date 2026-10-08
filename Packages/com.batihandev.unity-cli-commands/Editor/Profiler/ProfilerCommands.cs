using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using BatihanDev.UnityCliCommands.Profiler.Internal;
using BatihanDev.UnityCliCommands.Profiler.Schemas;
using Unity.Pipeline.Commands;
using UnityEngine;

namespace BatihanDev.UnityCliCommands.Profiler
{
    public static class ProfilerCommands
    {
        private static readonly string[] CaptureExtensions = { ".raw", ".data" };
        private static readonly ProfilerSessionStore Sessions = new ProfilerSessionStore(
            new UnityProfilerDataAdapter(), new SessionStateProfilerSessionPersistence());

        [CliCommand(
            "profiler.session.current",
            "Register the stopped in-memory Profiler history as an explicit session.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<ProfilerSessionResult> CurrentSession()
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<ProfilerSessionResult>.Failure(ProfilerSchema.Session, compatibility.Error);
            return Sessions.RegisterCurrent();
        }

        [CliCommand(
            "profiler.session.open",
            "Discover captures or replace Profiler history from one explicit project-relative capture.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<ProfilerSessionResult> OpenSession(
            [CliArg("path", "Optional project-relative .raw or .data capture path.")] string path = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<ProfilerSessionResult>.Failure(ProfilerSchema.Session, compatibility.Error);

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(path))
            {
                var discovery = DiscoverCaptureCandidates(projectRoot);
                if (!discovery.Ok)
                    return CommandResult<ProfilerSessionResult>.Failure(ProfilerSchema.Session, discovery.Error);
                var candidates = discovery.Result;
                var code = candidates.Count == 0
                    ? ProfilerErrorCode.SessionNotFound
                    : ProfilerErrorCode.SessionSelectionRequired;
                return CommandResult<ProfilerSessionResult>.Failure(
                    ProfilerSchema.Session,
                    code,
                    candidates.Count == 0
                        ? "No project capture is available."
                        : "Choose one project-relative capture path explicitly.",
                    new Dictionary<string, object> { ["candidates"] = candidates });
            }

            return OpenExplicitCapture(path, projectRoot, Sessions);
        }

        internal static CommandResult<ProfilerSessionResult> OpenExplicitCapture(
            string path,
            string projectRoot,
            ProfilerSessionStore sessions,
            Func<string, Stream> openRead = null)
        {
            var validated = ProjectPathPolicy.ValidateProjectFile(path, projectRoot, CaptureExtensions, openRead);
            if (!validated.Ok)
                return CommandResult<ProfilerSessionResult>.Failure(ProfilerSchema.Session, validated.Error);
            var fullPath = Path.GetFullPath(Path.Combine(projectRoot,
                validated.Result.Path.Replace('/', Path.DirectorySeparatorChar)));
            return sessions.OpenCapture(fullPath, validated.Result.Path);
        }

        [CliCommand(
            "profiler.time.frame-total",
            "Return ordered total-time hierarchy rows for one session frame and thread.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<FrameTotalResult> FrameTotal(
            [CliArg("sessionId", "Session ID from profiler.session.current or profiler.session.open.", Required = true)]
            string sessionId,
            [CliArg("frameIndex", "Exact Profiler frame index.", Required = true)] int frameIndex,
            [CliArg("targetMs", "Positive finite frame budget in milliseconds.", Required = true)] double targetMs,
            [CliArg("threadIndex", "Optional exact frame-local thread index.")] int? threadIndex = null,
            [CliArg("threadName", "Optional exact thread name.")] string threadName = null,
            [CliArg("limit", "Optional row limit from 1 through 200; defaults to 20.")] int? limit = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<FrameTotalResult>.Failure(ProfilerSchema.FrameTotal, compatibility.Error);
            return Sessions.FrameTotal(sessionId, frameIndex, targetMs, threadIndex, threadName, limit);
        }

        [CliCommand("profiler.time.range", "Return inclusive frame-range timing statistics.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<TimeRangeResult> TimeRange(
            [CliArg("sessionId", "Session ID from a profiler session command.", Required = true)] string sessionId,
            [CliArg("firstFrameIndex", "Inclusive first frame index.", Required = true)] int firstFrameIndex,
            [CliArg("lastFrameIndex", "Inclusive last frame index.", Required = true)] int lastFrameIndex,
            [CliArg("targetMs", "Positive finite frame budget in milliseconds.", Required = true)] double targetMs)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<TimeRangeResult>.Failure(ProfilerSchema.TimeRange, compatibility.Error);
            return Sessions.TimeRange(sessionId, firstFrameIndex, lastFrameIndex, targetMs);
        }

        [CliCommand("profiler.time.frame-self", "Return ordered self-time rows for one frame and thread.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<FrameSelfResult> FrameSelf(
            [CliArg("sessionId", "Session ID from a profiler session command.", Required = true)] string sessionId,
            [CliArg("frameIndex", "Exact Profiler frame index.", Required = true)] int frameIndex,
            [CliArg("threadIndex", "Optional exact frame-local thread index.")] int? threadIndex = null,
            [CliArg("threadName", "Optional exact thread name.")] string threadName = null,
            [CliArg("limit", "Optional row limit from 1 through 200; defaults to 20.")] int? limit = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<FrameSelfResult>.Failure(ProfilerSchema.FrameSelf, compatibility.Error);
            return Sessions.FrameSelf(sessionId, frameIndex, threadIndex, threadName, limit);
        }

        [CliCommand("profiler.time.sample", "Return one normal-hierarchy sample and its reconciliation data.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<SampleTimeResult> Sample(
            [CliArg("sessionId", "Session ID from a profiler session command.", Required = true)] string sessionId,
            [CliArg("frameIndex", "Exact Profiler frame index.", Required = true)] int frameIndex,
            [CliArg("sampleId", "Normal sample ID from a timing result.", Required = true)] string sampleId,
            [CliArg("threadIndex", "Optional exact frame-local thread index.")] int? threadIndex = null,
            [CliArg("threadName", "Optional exact thread name.")] string threadName = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<SampleTimeResult>.Failure(ProfilerSchema.SampleTime, compatibility.Error);
            return Sessions.Sample(sessionId, frameIndex, sampleId, threadIndex, threadName);
        }

        [CliCommand("profiler.time.bottom-up", "Return one inverted-hierarchy marker and caller reconciliation.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<BottomUpResult> BottomUp(
            [CliArg("sessionId", "Session ID from a profiler session command.", Required = true)] string sessionId,
            [CliArg("frameIndex", "Exact Profiler frame index.", Required = true)] int frameIndex,
            [CliArg("bottomUpId", "Bottom-up ID from profiler.time.frame-self.", Required = true)] string bottomUpId,
            [CliArg("threadIndex", "Optional exact frame-local thread index.")] int? threadIndex = null,
            [CliArg("threadName", "Optional exact thread name.")] string threadName = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<BottomUpResult>.Failure(ProfilerSchema.BottomUp, compatibility.Error);
            return Sessions.BottomUp(sessionId, frameIndex, bottomUpId, threadIndex, threadName);
        }

        [CliCommand("profiler.time.marker-path", "Resolve one exact semantic marker path.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<MarkerTimeResult> MarkerPath(
            [CliArg("sessionId", "Session ID from a profiler session command.", Required = true)] string sessionId,
            [CliArg("frameIndex", "Exact Profiler frame index.", Required = true)] int frameIndex,
            [CliArg("markerPath", "Exact ordered marker-name path.", Required = true)] string[] markerPath,
            [CliArg("threadIndex", "Optional exact frame-local thread index.")] int? threadIndex = null,
            [CliArg("threadName", "Optional exact thread name.")] string threadName = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<MarkerTimeResult>.Failure(ProfilerSchema.MarkerTime, compatibility.Error);
            return Sessions.MarkerPath(sessionId, frameIndex, markerPath, threadIndex, threadName);
        }

        [CliCommand("profiler.time.related", "Return positive temporal overlaps on one exact target thread.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<RelatedTimeResult> Related(
            [CliArg("sessionId", "Session ID from a profiler session command.", Required = true)] string sessionId,
            [CliArg("frameIndex", "Exact Profiler frame index.", Required = true)] int frameIndex,
            [CliArg("sourceSampleId", "Raw occurrence ID from profiler.time.sample.", Required = true)] string sourceSampleId,
            [CliArg("sourceThreadIndex", "Optional exact source thread index.")] int? sourceThreadIndex = null,
            [CliArg("sourceThreadName", "Optional exact source thread name.")] string sourceThreadName = null,
            [CliArg("targetThreadIndex", "Optional exact target thread index.")] int? targetThreadIndex = null,
            [CliArg("targetThreadName", "Optional exact target thread name.")] string targetThreadName = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<RelatedTimeResult>.Failure(ProfilerSchema.RelatedTime, compatibility.Error);
            return Sessions.Related(sessionId, frameIndex, sourceSampleId, sourceThreadIndex, sourceThreadName,
                targetThreadIndex, targetThreadName);
        }

        [CliCommand("profiler.gc.overall", "Return full-session allocation totals and thresholded sites.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<GcOverallResult> GcOverall(
            [CliArg("sessionId", "Session ID from a profiler session command.", Required = true)] string sessionId,
            [CliArg("thresholdBytes", "Non-negative direct site threshold in bytes; defaults to 8192.")]
            long thresholdBytes = 8192)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<GcOverallResult>.Failure(ProfilerSchema.GcOverall, compatibility.Error);
            return Sessions.GcOverall(sessionId, thresholdBytes);
        }

        [CliCommand("profiler.gc.frame", "Return one frame/thread allocation summary and normal site IDs.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<GcFrameResult> GcFrame(
            [CliArg("sessionId", "Session ID from a profiler session command.", Required = true)] string sessionId,
            [CliArg("frameIndex", "Exact Profiler frame index.", Required = true)] int frameIndex,
            [CliArg("threadIndex", "Optional exact frame-local thread index.")] int? threadIndex = null,
            [CliArg("threadName", "Optional exact thread name.")] string threadName = null,
            [CliArg("thresholdBytes", "Non-negative direct site threshold in bytes; defaults to 8192.")]
            long thresholdBytes = 8192)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<GcFrameResult>.Failure(ProfilerSchema.GcFrame, compatibility.Error);
            return Sessions.GcFrame(sessionId, frameIndex, threadIndex, threadName, thresholdBytes);
        }

        [CliCommand("profiler.gc.range", "Return inclusive all-thread frame-range allocation statistics.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<GcRangeResult> GcRange(
            [CliArg("sessionId", "Session ID from a profiler session command.", Required = true)] string sessionId,
            [CliArg("firstFrameIndex", "Inclusive first frame index.", Required = true)] int firstFrameIndex,
            [CliArg("lastFrameIndex", "Inclusive last frame index.", Required = true)] int lastFrameIndex,
            [CliArg("thresholdBytes", "Non-negative direct site threshold in bytes; defaults to 8192.")]
            long thresholdBytes = 8192)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<GcRangeResult>.Failure(ProfilerSchema.GcRange, compatibility.Error);
            return Sessions.GcRange(sessionId, firstFrameIndex, lastFrameIndex, thresholdBytes);
        }

        [CliCommand("profiler.gc.sample", "Return inclusive and direct allocations for one normal sample ID.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<GcAttributionResult> GcSample(
            [CliArg("sessionId", "Session ID from a profiler session command.", Required = true)] string sessionId,
            [CliArg("frameIndex", "Exact Profiler frame index.", Required = true)] int frameIndex,
            [CliArg("sampleId", "Normal sample ID from a profiler result.", Required = true)] string sampleId,
            [CliArg("threadIndex", "Optional exact frame-local thread index.")] int? threadIndex = null,
            [CliArg("threadName", "Optional exact thread name.")] string threadName = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<GcAttributionResult>.Failure(ProfilerSchema.GcSample, compatibility.Error);
            return Sessions.GcSample(sessionId, frameIndex, sampleId, threadIndex, threadName);
        }

        [CliCommand("profiler.gc.marker-path", "Return inclusive and direct allocations for one exact marker path.",
            Tags = new[] { "unity-cli-commands", "profiler" })]
        public static CommandResult<GcAttributionResult> GcMarkerPath(
            [CliArg("sessionId", "Session ID from a profiler session command.", Required = true)] string sessionId,
            [CliArg("frameIndex", "Exact Profiler frame index.", Required = true)] int frameIndex,
            [CliArg("markerPath", "Exact ordered marker-name path.", Required = true)] string[] markerPath,
            [CliArg("threadIndex", "Optional exact frame-local thread index.")] int? threadIndex = null,
            [CliArg("threadName", "Optional exact thread name.")] string threadName = null)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<GcAttributionResult>.Failure(ProfilerSchema.GcMarker, compatibility.Error);
            return Sessions.GcMarkerPath(sessionId, frameIndex, markerPath, threadIndex, threadName);
        }

        internal static CommandResult<IReadOnlyList<string>> DiscoverCaptureCandidates(
            string projectRoot,
            Func<string, IEnumerable<string>> enumerateEntries = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(projectRoot) ||
                    (enumerateEntries == null && !Directory.Exists(projectRoot)))
                    return CommandResult<IReadOnlyList<string>>.Success(
                        ProfilerSchema.Session, Array.Empty<string>());
                var enumerate = enumerateEntries ?? Directory.EnumerateFileSystemEntries;
                var candidates = new List<string>();
                var pending = new Stack<string>();
                pending.Push(Path.GetFullPath(projectRoot));
                while (pending.Count > 0)
                {
                    var directory = pending.Pop();
                    foreach (var entry in enumerate(directory))
                    {
                        var attributes = File.GetAttributes(entry);
                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                            continue;
                        if ((attributes & FileAttributes.Directory) != 0)
                            pending.Push(entry);
                        else if (CaptureExtensions.Contains(Path.GetExtension(entry),
                                     StringComparer.OrdinalIgnoreCase))
                            candidates.Add(Path.GetRelativePath(projectRoot, entry).Replace('\\', '/'));
                    }
                }
                candidates.Sort(StringComparer.Ordinal);
                return CommandResult<IReadOnlyList<string>>.Success(ProfilerSchema.Session, candidates);
            }
            catch (Exception exception) when (ProjectPathPolicy.IsExpectedPathException(exception))
            {
                return ProjectPathPolicy.FileSystemFailure<IReadOnlyList<string>>(
                    ProfilerSchema.Session, projectRoot, exception);
            }
        }
    }
}
