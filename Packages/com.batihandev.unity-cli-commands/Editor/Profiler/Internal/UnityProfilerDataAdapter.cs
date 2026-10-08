using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace BatihanDev.UnityCliCommands.Profiler.Internal
{
    internal sealed class UnityProfilerDataAdapter : IProfilerDataAdapter
    {
        public UnityProfilerDataAdapter()
        {
            ProfilerDriver.profileLoaded += OnHistoryChanged;
            ProfilerDriver.profileCleared += OnHistoryChanged;
            ProfilerDriver.NewProfilerFrameRecorded += OnNewProfilerFrameRecorded;
        }

        public bool IsRecording => ProfilerDriver.enabled;

        public event Action HistoryChanged;

        public IReadOnlyList<ProfilerFrameRecord> ReadAllFrames()
        {
            var frames = new List<ProfilerFrameRecord>();
            var frameIndex = ProfilerDriver.firstFrameIndex;
            var lastFrameIndex = ProfilerDriver.lastFrameIndex;
            while (frameIndex >= 0 && frameIndex <= lastFrameIndex)
            {
                var frame = ReadFrame(frameIndex);
                if (frame != null)
                    frames.Add(frame);
                var next = ProfilerDriver.GetNextFrameIndex(frameIndex);
                if (next <= frameIndex)
                    break;
                frameIndex = next;
            }
            return frames;
        }

        public bool LoadProfile(string capturePath)
        {
            return ProfilerDriver.LoadProfile(capturePath, false);
        }

        private static ProfilerFrameRecord ReadFrame(int frameIndex)
        {
            var threads = new List<ProfilerThreadRecord>();
            long frameStartTimeNs = 0;
            long frameTimeNs = 0;
            for (var threadIndex = 0; ; threadIndex++)
            {
                using (var view = ProfilerDriver.GetHierarchyFrameDataView(frameIndex, threadIndex,
                           HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnDontSort, false))
                {
                    if (!view.valid)
                        break;
                    frameStartTimeNs = NanosecondsToLong(view.frameStartTimeNs);
                    frameTimeNs = NanosecondsToLong(view.frameTimeNs);
                    threads.Add(ReadThread(view));
                }
            }
            return threads.Count == 0
                ? null
                : new ProfilerFrameRecord(frameIndex, frameStartTimeNs, frameTimeNs, threads);
        }

        private static ProfilerThreadRecord ReadThread(HierarchyFrameDataView view)
        {
            var samples = new List<ProfilerSampleRecord>();
            ReadItem(view, view.GetRootItemID(), null, samples);
            var rawSamples = ReadRawSamples(view.frameIndex, view.threadIndex, out var allocationDataAvailable);
            var bottomUpSamples = ReadBottomUpSamples(view.frameIndex, view.threadIndex);
            return new ProfilerThreadRecord(view.threadIndex, view.threadId, view.threadGroupName, view.threadName,
                allocationDataAvailable, samples, rawSamples, bottomUpSamples);
        }

        private static IReadOnlyList<ProfilerSampleRecord> ReadRawSamples(
            int frameIndex, int threadIndex, out bool allocationDataAvailable)
        {
            using (var view = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex))
            {
                if (!view.valid)
                    throw new ProfilerDataUnavailableException("Raw profiler occurrence data is unavailable.");
                var samples = new List<ProfilerSampleRecord>();
                var gcAllocationMarkerId = view.GetMarkerId("GC.Alloc");
                allocationDataAvailable = true;
                if (view.sampleCount > 0)
                    ReadRawItem(view, 0, null, gcAllocationMarkerId, samples, ref allocationDataAvailable);
                return samples;
            }
        }

        private static int ReadRawItem(RawFrameDataView view, int sampleIndex, int? parentIndex,
            int gcAllocationMarkerId, List<ProfilerSampleRecord> samples, ref bool allocationDataAvailable)
        {
            var flows = new List<RawFrameDataView.FlowEvent>();
            view.GetSampleFlowEvents(sampleIndex, flows);
            var markerId = view.GetSampleMarkerId(sampleIndex);
            long allocationBytes = 0;
            long allocationCount = 0;
            if (markerId == gcAllocationMarkerId)
            {
                if (view.GetSampleMetadataCount(sampleIndex) < 1)
                    allocationDataAvailable = false;
                else
                {
                    allocationBytes = view.GetSampleMetadataAsLong(sampleIndex, 0);
                    allocationCount = 1;
                }
            }
            samples.Add(new ProfilerSampleRecord(sampleIndex, parentIndex, view.GetSampleName(sampleIndex),
                NanosecondsToLong(view.GetSampleStartTimeNs(sampleIndex)),
                NanosecondsToLong(view.GetSampleTimeNs(sampleIndex)),
                RawSelfTime(view, sampleIndex), 1, allocationBytes, allocationCount, false,
                flows.Select(value => (long)value.FlowId).Distinct().OrderBy(value => value).ToArray(),
                markerId, new[] { sampleIndex }));
            var next = sampleIndex + 1;
            for (var child = 0; child < view.GetSampleChildrenCount(sampleIndex); child++)
                next = ReadRawItem(view, next, sampleIndex, gcAllocationMarkerId, samples,
                    ref allocationDataAvailable);
            return next;
        }

        private static long RawSelfTime(RawFrameDataView view, int sampleIndex)
        {
            var self = NanosecondsToLong(view.GetSampleTimeNs(sampleIndex));
            var childIndex = sampleIndex + 1;
            for (var child = 0; child < view.GetSampleChildrenCount(sampleIndex); child++)
            {
                self -= NanosecondsToLong(view.GetSampleTimeNs(childIndex));
                childIndex += view.GetSampleChildrenCountRecursive(childIndex) + 1;
            }
            return Math.Max(0, self);
        }

        private static IReadOnlyList<ProfilerSampleRecord> ReadBottomUpSamples(int frameIndex, int threadIndex)
        {
            using (var view = ProfilerDriver.GetHierarchyFrameDataView(frameIndex, threadIndex,
                       HierarchyFrameDataView.ViewModes.InvertHierarchy,
                       HierarchyFrameDataView.columnDontSort, false))
            {
                if (!view.valid)
                    throw new ProfilerDataUnavailableException("Inverted profiler hierarchy is unavailable.");
                var samples = new List<ProfilerSampleRecord>();
                ReadItem(view, view.GetRootItemID(), null, samples);
                return samples;
            }
        }

        private static void ReadItem(HierarchyFrameDataView view, int itemId, int? parentId,
            List<ProfilerSampleRecord> samples)
        {
            var rawSampleIndices = new List<int>();
            view.GetItemRawFrameDataViewIndices(itemId, rawSampleIndices);
            samples.Add(new ProfilerSampleRecord(itemId, parentId, view.GetItemName(itemId),
                MillisecondsToNanoseconds(view.GetItemColumnDataAsSingle(itemId, HierarchyFrameDataView.columnStartTime)),
                MillisecondsToNanoseconds(view.GetItemColumnDataAsSingle(itemId, HierarchyFrameDataView.columnTotalTime)),
                MillisecondsToNanoseconds(view.GetItemColumnDataAsSingle(itemId, HierarchyFrameDataView.columnSelfTime)),
                ReadCalls(view, itemId), 0, 0, false,
                Array.Empty<long>(), view.GetItemMarkerID(itemId), rawSampleIndices));
            var children = new List<int>();
            view.GetItemChildren(itemId, children);
            foreach (var child in children)
                ReadItem(view, child, itemId, samples);
        }

        private static long MillisecondsToNanoseconds(float milliseconds)
        {
            if (float.IsNaN(milliseconds) || float.IsInfinity(milliseconds) || milliseconds < 0f ||
                milliseconds > long.MaxValue / 1000000d)
                throw new ProfilerDataUnavailableException("Profiler timing is unavailable as finite nanoseconds.");
            return (long)Math.Round(milliseconds * 1000000d, MidpointRounding.AwayFromZero);
        }

        private static long NanosecondsToLong(ulong nanoseconds)
        {
            if (nanoseconds > long.MaxValue)
                throw new ProfilerDataUnavailableException("Profiler timing exceeds the supported nanosecond range.");
            return (long)nanoseconds;
        }

        private static int ReadCalls(HierarchyFrameDataView view, int itemId)
        {
            var calls = view.GetItemColumnDataAsDouble(itemId, HierarchyFrameDataView.columnCalls);
            if (double.IsNaN(calls) || double.IsInfinity(calls) || calls < 0d || calls > int.MaxValue ||
                Math.Truncate(calls) != calls)
                throw new ProfilerDataUnavailableException("Profiler call count is unavailable as an exact integer.");
            return (int)calls;
        }

        private void OnHistoryChanged()
        {
            HistoryChanged?.Invoke();
        }

        private void OnNewProfilerFrameRecorded(int firstFrameIndex, int lastFrameIndex)
        {
            HistoryChanged?.Invoke();
        }
    }
}
