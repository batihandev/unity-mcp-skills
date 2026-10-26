using System;
using System.Collections.Generic;

namespace BatihanDev.UnityCliCommands.Profiler.Internal
{
    public enum ProfilerViewKind
    {
        Normal,
        Raw,
        BottomUp
    }

    public sealed class ProfilerFrameRecord
    {
        public ProfilerFrameRecord(
            int frameIndex,
            long frameStartTimeNs,
            long frameTimeNs,
            IReadOnlyList<ProfilerThreadRecord> threads)
        {
            FrameIndex = frameIndex;
            FrameStartTimeNs = frameStartTimeNs;
            FrameTimeNs = frameTimeNs;
            Threads = threads ?? Array.Empty<ProfilerThreadRecord>();
        }

        public int FrameIndex { get; }
        public long FrameStartTimeNs { get; }
        public long FrameTimeNs { get; }
        public IReadOnlyList<ProfilerThreadRecord> Threads { get; }
        public double FrameTimeMs => FrameTimeNs / 1000000d;
    }

    public sealed class ProfilerThreadRecord
    {
        public ProfilerThreadRecord(
            int threadIndex,
            ulong threadId,
            string threadGroupName,
            string threadName,
            bool allocationDataAvailable,
            IReadOnlyList<ProfilerSampleRecord> samples,
            IReadOnlyList<ProfilerSampleRecord> rawSamples = null,
            IReadOnlyList<ProfilerSampleRecord> bottomUpSamples = null)
        {
            ThreadIndex = threadIndex;
            ThreadId = threadId;
            ThreadGroupName = threadGroupName ?? string.Empty;
            ThreadName = threadName ?? string.Empty;
            AllocationDataAvailable = allocationDataAvailable;
            Samples = samples ?? Array.Empty<ProfilerSampleRecord>();
            RawSamples = rawSamples ?? Samples;
            BottomUpSamples = bottomUpSamples ?? Samples;
        }

        public int ThreadIndex { get; }
        public ulong ThreadId { get; }
        public string ThreadGroupName { get; }
        public string ThreadName { get; }
        public bool AllocationDataAvailable { get; }
        public IReadOnlyList<ProfilerSampleRecord> Samples { get; }
        public IReadOnlyList<ProfilerSampleRecord> RawSamples { get; }
        public IReadOnlyList<ProfilerSampleRecord> BottomUpSamples { get; }
    }

    public sealed class ProfilerSampleRecord
    {
        public ProfilerSampleRecord(
            int itemId,
            int? parentItemId,
            string markerName,
            long startTimeNs,
            long totalTimeNs,
            long selfTimeNs,
            int calls,
            long allocationBytes,
            long allocationCount,
            bool nativeMarker,
            IReadOnlyList<long> flowIds,
            int markerId = -1,
            IReadOnlyList<int> rawSampleIndices = null)
        {
            ItemId = itemId;
            ParentItemId = parentItemId;
            MarkerName = markerName ?? string.Empty;
            StartTimeNs = startTimeNs;
            TotalTimeNs = totalTimeNs;
            SelfTimeNs = selfTimeNs;
            Calls = calls;
            AllocationBytes = allocationBytes;
            AllocationCount = allocationCount;
            NativeMarker = nativeMarker;
            FlowIds = flowIds ?? Array.Empty<long>();
            MarkerId = markerId;
            RawSampleIndices = rawSampleIndices ?? Array.Empty<int>();
        }

        public int ItemId { get; }
        public int? ParentItemId { get; }
        public string MarkerName { get; }
        public long StartTimeNs { get; }
        public long TotalTimeNs { get; }
        public long SelfTimeNs { get; }
        public int Calls { get; }
        public long AllocationBytes { get; }
        public long AllocationCount { get; }
        public bool NativeMarker { get; }
        public IReadOnlyList<long> FlowIds { get; }
        public int MarkerId { get; }
        public IReadOnlyList<int> RawSampleIndices { get; }
        public double TotalMs => TotalTimeNs / 1000000d;
        public double SelfMs => SelfTimeNs / 1000000d;
    }

    public sealed class ProfilerOpaqueIdPayload
    {
        public ProfilerOpaqueIdPayload(
            int version,
            long historyEpoch,
            ProfilerViewKind viewKind,
            int frameIndex,
            int threadIndex,
            int itemId)
        {
            Version = version;
            HistoryEpoch = historyEpoch;
            ViewKind = viewKind;
            FrameIndex = frameIndex;
            ThreadIndex = threadIndex;
            ItemId = itemId;
        }

        public int Version { get; }
        public long HistoryEpoch { get; }
        public ProfilerViewKind ViewKind { get; }
        public int FrameIndex { get; }
        public int ThreadIndex { get; }
        public int ItemId { get; }
    }
}
