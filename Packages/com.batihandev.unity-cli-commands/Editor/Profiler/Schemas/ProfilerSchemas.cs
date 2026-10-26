using System;
using System.Collections.Generic;

namespace BatihanDev.UnityCliCommands.Profiler.Schemas
{
    public static class ProfilerSchema
    {
        public const string Session = "unity.profiler.session@1";
        public const string TimeRange = "unity.profiler.time-range@1";
        public const string FrameTotal = "unity.profiler.frame-total@1";
        public const string FrameSelf = "unity.profiler.frame-self@1";
        public const string SampleTime = "unity.profiler.sample-time@1";
        public const string BottomUp = "unity.profiler.bottom-up@1";
        public const string MarkerTime = "unity.profiler.marker-time@1";
        public const string GcOverall = "unity.profiler.gc-overall@1";
        public const string GcFrame = "unity.profiler.gc-frame@1";
        public const string GcRange = "unity.profiler.gc-range@1";
        public const string GcSample = "unity.profiler.gc-sample@1";
        public const string GcMarker = "unity.profiler.gc-marker@1";
        public const string RelatedTime = "unity.profiler.related-time@1";
        public const string Investigation = "unity.profiler.investigation@1";
        public const string OpaqueId = "unity.profiler.opaque-id@1";
    }

    public static class ProfilerErrorCode
    {
        public const string TargetMsInvalid = "TARGET_MS_INVALID";
        public const string FrameRangeInvalid = "FRAME_RANGE_INVALID";
        public const string FrameRangeGap = "FRAME_RANGE_GAP";
        public const string ThreadSelectorConflict = "THREAD_SELECTOR_CONFLICT";
        public const string ThreadNotFound = "THREAD_NOT_FOUND";
        public const string ThreadAmbiguous = "THREAD_AMBIGUOUS";
        public const string MarkerPathNotFound = "MARKER_PATH_NOT_FOUND";
        public const string MarkerPathAmbiguous = "MARKER_PATH_AMBIGUOUS";
        public const string SampleIdInvalid = "SAMPLE_ID_INVALID";
        public const string SampleIdStale = "SAMPLE_ID_STALE";
        public const string SampleIdKindMismatch = "SAMPLE_ID_KIND_MISMATCH";
        public const string AllocationDataUnavailable = "ALLOCATION_DATA_UNAVAILABLE";
        public const string ThresholdBytesInvalid = "THRESHOLD_BYTES_INVALID";
        public const string SessionNotFound = "SESSION_NOT_FOUND";
        public const string SessionSelectionRequired = "SESSION_SELECTION_REQUIRED";
        public const string SessionExpired = "SESSION_EXPIRED";
        public const string SessionMismatch = "SESSION_MISMATCH";
        public const string ProfilerHistoryMutable = "PROFILER_HISTORY_MUTABLE";
        public const string CaptureInvalid = "CAPTURE_INVALID";
        public const string CaptureUnsupported = "CAPTURE_UNSUPPORTED";
        public const string ProfilerDataUnavailable = "PROFILER_DATA_UNAVAILABLE";
        public const string FrameNotFound = "FRAME_NOT_FOUND";
        public const string LimitInvalid = "LIMIT_INVALID";
    }

    [Serializable]
    public sealed class ProfilerSessionResult
    {
        public ProfilerSessionResult(string sessionId, string source, string capturePath, int firstFrameIndex,
            int lastFrameIndex, int frameCount, long historyEpoch, string fingerprint)
        {
            SessionId = sessionId;
            Source = source;
            CapturePath = capturePath;
            FirstFrameIndex = firstFrameIndex;
            LastFrameIndex = lastFrameIndex;
            FrameCount = frameCount;
            HistoryEpoch = historyEpoch;
            Fingerprint = fingerprint;
        }

        public string SessionId { get; }
        public string Source { get; }
        public string CapturePath { get; }
        public int FirstFrameIndex { get; }
        public int LastFrameIndex { get; }
        public int FrameCount { get; }
        public long HistoryEpoch { get; }
        public string Fingerprint { get; }
        public bool MutatesProfilerHistory => Source == "capture";
        public bool MutatesProjectAssets => false;
        public bool Undoable => false;
    }

    [Serializable]
    public sealed class TimeRangeResult
    {
        public TimeRangeResult(string sessionId, int firstFrameIndex, int lastFrameIndex, double targetMs, int frameCount,
            double minMs, double medianMs, double p95Ms, double maxMs, int maxFrameIndex,
            int overBudgetFrameCount, double overBudgetPercent, double maxOverBudgetMs, double maxBudgetRatio)
        {
            SessionId = sessionId;
            FirstFrameIndex = firstFrameIndex;
            LastFrameIndex = lastFrameIndex;
            TargetMs = targetMs;
            FrameCount = frameCount;
            MinMs = minMs;
            MedianMs = medianMs;
            P95Ms = p95Ms;
            MaxMs = maxMs;
            MaxFrameIndex = maxFrameIndex;
            OverBudgetFrameCount = overBudgetFrameCount;
            OverBudgetPercent = overBudgetPercent;
            MaxOverBudgetMs = maxOverBudgetMs;
            MaxBudgetRatio = maxBudgetRatio;
        }

        public string SessionId { get; }
        public int FirstFrameIndex { get; }
        public int LastFrameIndex { get; }
        public double TargetMs { get; }
        public int FrameCount { get; }
        public double MinMs { get; }
        public double MedianMs { get; }
        public double P95Ms { get; }
        public double MaxMs { get; }
        public int MaxFrameIndex { get; }
        public int OverBudgetFrameCount { get; }
        public double OverBudgetPercent { get; }
        public double MaxOverBudgetMs { get; }
        public double MaxBudgetRatio { get; }
        public string PercentileMethod => "nearest-rank";
    }

    [Serializable]
    public sealed class ThreadIdentity
    {
        public ThreadIdentity(int threadIndex, ulong threadId, string threadGroupName, string threadName)
        {
            ThreadIndex = threadIndex;
            ThreadId = threadId;
            ThreadGroupName = threadGroupName;
            ThreadName = threadName;
        }

        public int ThreadIndex { get; }
        public ulong ThreadId { get; }
        public string ThreadGroupName { get; }
        public string ThreadName { get; }
    }

    [Serializable]
    public sealed class TimeRow
    {
        public TimeRow(string sampleId, IReadOnlyList<string> markerPath, double totalMs, double selfMs,
            int calls, double? percentOfFrame, double percentOfBudget)
        {
            SampleId = sampleId;
            MarkerPath = markerPath;
            TotalMs = totalMs;
            SelfMs = selfMs;
            Calls = calls;
            PercentOfFrame = percentOfFrame;
            PercentOfBudget = percentOfBudget;
        }

        public string SampleId { get; }
        public IReadOnlyList<string> MarkerPath { get; }
        public double TotalMs { get; }
        public double SelfMs { get; }
        public int Calls { get; }
        public double? PercentOfFrame { get; }
        public double PercentOfBudget { get; }
    }

    [Serializable]
    public sealed class FrameTotalResult
    {
        public FrameTotalResult(string sessionId, int frameIndex, double targetMs, double frameTimeMs,
            ThreadIdentity thread, IReadOnlyList<TimeRow> rows, int availableRowCount)
        {
            SessionId = sessionId;
            FrameIndex = frameIndex;
            TargetMs = targetMs;
            FrameTimeMs = frameTimeMs;
            OverBudget = frameTimeMs > targetMs;
            OverBudgetMs = Math.Max(0d, frameTimeMs - targetMs);
            BudgetRatio = frameTimeMs / targetMs;
            Thread = thread;
            Rows = rows;
            AvailableRowCount = availableRowCount;
            Truncated = rows.Count < availableRowCount;
        }

        public string SessionId { get; }
        public int FrameIndex { get; }
        public double TargetMs { get; }
        public double FrameTimeMs { get; }
        public bool OverBudget { get; }
        public double OverBudgetMs { get; }
        public double BudgetRatio { get; }
        public ThreadIdentity Thread { get; }
        public IReadOnlyList<TimeRow> Rows { get; }
        public int AvailableRowCount { get; }
        public bool Truncated { get; }
    }

    [Serializable]
    public sealed class GcAggregateRow
    {
        public GcAggregateRow(IReadOnlyList<string> markerPath, long bytes, long count)
        {
            MarkerPath = markerPath;
            Bytes = bytes;
            Count = count;
        }
        public IReadOnlyList<string> MarkerPath { get; }
        public long Bytes { get; }
        public long Count { get; }
    }

    [Serializable]
    public sealed class GcSiteRow
    {
        public GcSiteRow(string sampleId, IReadOnlyList<string> markerPath, long bytes, long count)
        {
            SampleId = sampleId;
            MarkerPath = markerPath;
            Bytes = bytes;
            Count = count;
        }
        public string SampleId { get; }
        public IReadOnlyList<string> MarkerPath { get; }
        public long Bytes { get; }
        public long Count { get; }
    }

    [Serializable]
    public sealed class GcFrameResult
    {
        public GcFrameResult(string sessionId, int frameIndex, ThreadIdentity thread, long thresholdBytes,
            long totalBytes, long totalCount, long reportedBytes, long reportedCount,
            IReadOnlyList<GcSiteRow> rows)
        {
            SessionId = sessionId; FrameIndex = frameIndex; Thread = thread; ThresholdBytes = thresholdBytes;
            TotalBytes = totalBytes; TotalCount = totalCount; ReportedBytes = reportedBytes;
            ReportedCount = reportedCount; Rows = rows;
        }
        public string SessionId { get; }
        public int FrameIndex { get; }
        public ThreadIdentity Thread { get; }
        public long ThresholdBytes { get; }
        public long TotalBytes { get; }
        public long TotalCount { get; }
        public long ReportedBytes { get; }
        public long ReportedCount { get; }
        public IReadOnlyList<GcSiteRow> Rows { get; }
    }

    [Serializable]
    public sealed class GcOverallResult
    {
        public GcOverallResult(string sessionId, int frameCount, long thresholdBytes, long totalBytes,
            long totalCount, long reportedBytes, long reportedCount, IReadOnlyList<GcAggregateRow> rows)
        {
            SessionId = sessionId; FrameCount = frameCount; ThresholdBytes = thresholdBytes;
            TotalBytes = totalBytes; TotalCount = totalCount; ReportedBytes = reportedBytes;
            ReportedCount = reportedCount; Rows = rows;
        }
        public string SessionId { get; }
        public int FrameCount { get; }
        public long ThresholdBytes { get; }
        public long TotalBytes { get; }
        public long TotalCount { get; }
        public long ReportedBytes { get; }
        public long ReportedCount { get; }
        public IReadOnlyList<GcAggregateRow> Rows { get; }
    }

    [Serializable]
    public sealed class GcFrameSummary
    {
        public GcFrameSummary(int frameIndex, long totalBytes, long totalCount,
            long reportedBytes, long reportedCount)
        {
            FrameIndex = frameIndex; TotalBytes = totalBytes; TotalCount = totalCount;
            ReportedBytes = reportedBytes; ReportedCount = reportedCount;
        }
        public int FrameIndex { get; }
        public long TotalBytes { get; }
        public long TotalCount { get; }
        public long ReportedBytes { get; }
        public long ReportedCount { get; }
    }

    [Serializable]
    public sealed class GcRangeResult
    {
        public GcRangeResult(string sessionId, int firstFrameIndex, int lastFrameIndex, int frameCount,
            long thresholdBytes, long totalBytes, long totalCount, long reportedBytes, long reportedCount,
            long minBytes, decimal medianBytes, long p95Bytes, long maxBytes, int maxFrameIndex,
            IReadOnlyList<GcFrameSummary> frames)
        {
            SessionId = sessionId; FirstFrameIndex = firstFrameIndex; LastFrameIndex = lastFrameIndex;
            FrameCount = frameCount; ThresholdBytes = thresholdBytes; TotalBytes = totalBytes;
            TotalCount = totalCount; ReportedBytes = reportedBytes; ReportedCount = reportedCount;
            MinBytes = minBytes; MedianBytes = medianBytes; P95Bytes = p95Bytes; MaxBytes = maxBytes;
            MaxFrameIndex = maxFrameIndex; Frames = frames;
        }
        public string SessionId { get; }
        public int FirstFrameIndex { get; }
        public int LastFrameIndex { get; }
        public int FrameCount { get; }
        public long ThresholdBytes { get; }
        public long TotalBytes { get; }
        public long TotalCount { get; }
        public long ReportedBytes { get; }
        public long ReportedCount { get; }
        public long MinBytes { get; }
        public decimal MedianBytes { get; }
        public long P95Bytes { get; }
        public long MaxBytes { get; }
        public int MaxFrameIndex { get; }
        public string PercentileMethod => "nearest-rank";
        public IReadOnlyList<GcFrameSummary> Frames { get; }
    }

    [Serializable]
    public sealed class GcAttributionResult
    {
        public GcAttributionResult(string sessionId, int frameIndex, ThreadIdentity thread, string sampleId,
            IReadOnlyList<string> markerPath, long totalBytes, long totalCount, long directBytes, long directCount)
        {
            SessionId = sessionId; FrameIndex = frameIndex; Thread = thread; SampleId = sampleId;
            MarkerPath = markerPath; TotalBytes = totalBytes; TotalCount = totalCount;
            DirectBytes = directBytes; DirectCount = directCount;
        }
        public string SessionId { get; }
        public int FrameIndex { get; }
        public ThreadIdentity Thread { get; }
        public string SampleId { get; }
        public IReadOnlyList<string> MarkerPath { get; }
        public long TotalBytes { get; }
        public long TotalCount { get; }
        public long DirectBytes { get; }
        public long DirectCount { get; }
    }

    [Serializable]
    public sealed class RelatedRow
    {
        public RelatedRow(string sampleId, IReadOnlyList<string> markerPath, double overlapMs, double startMs,
            string relation)
        {
            SampleId = sampleId;
            MarkerPath = markerPath;
            OverlapMs = overlapMs;
            StartMs = startMs;
            Relation = relation;
        }
        public string SampleId { get; }
        public IReadOnlyList<string> MarkerPath { get; }
        public double OverlapMs { get; }
        public double StartMs { get; }
        public string Relation { get; }
    }

    [Serializable]
    public sealed class RelatedTimeResult
    {
        public RelatedTimeResult(string sessionId, int frameIndex, string sourceSampleId,
            ThreadIdentity sourceThread, ThreadIdentity targetThread, IReadOnlyList<RelatedRow> rows)
        {
            SessionId = sessionId;
            FrameIndex = frameIndex;
            SourceSampleId = sourceSampleId;
            SourceThread = sourceThread;
            TargetThread = targetThread;
            Rows = rows;
        }
        public string SessionId { get; }
        public int FrameIndex { get; }
        public string SourceSampleId { get; }
        public ThreadIdentity SourceThread { get; }
        public ThreadIdentity TargetThread { get; }
        public IReadOnlyList<RelatedRow> Rows { get; }
    }

    [Serializable]
    public sealed class SelfTimeRow
    {
        public SelfTimeRow(string bottomUpId, IReadOnlyList<IReadOnlyList<string>> markerPaths,
            double totalMs, double selfMs, int calls)
        {
            BottomUpId = bottomUpId;
            MarkerPaths = markerPaths;
            TotalMs = totalMs;
            SelfMs = selfMs;
            Calls = calls;
        }
        public string BottomUpId { get; }
        public IReadOnlyList<IReadOnlyList<string>> MarkerPaths { get; }
        public double TotalMs { get; }
        public double SelfMs { get; }
        public int Calls { get; }
    }

    [Serializable]
    public sealed class FrameSelfResult
    {
        public FrameSelfResult(string sessionId, int frameIndex, ThreadIdentity thread,
            IReadOnlyList<SelfTimeRow> rows, int availableRowCount)
        {
            SessionId = sessionId;
            FrameIndex = frameIndex;
            Thread = thread;
            Rows = rows;
            AvailableRowCount = availableRowCount;
            Truncated = rows.Count < availableRowCount;
        }
        public string SessionId { get; }
        public int FrameIndex { get; }
        public ThreadIdentity Thread { get; }
        public IReadOnlyList<SelfTimeRow> Rows { get; }
        public int AvailableRowCount { get; }
        public bool Truncated { get; }
    }

    [Serializable]
    public sealed class SampleSummary
    {
        public SampleSummary(string sampleId, IReadOnlyList<string> markerPath, double totalMs, double selfMs,
            int calls)
        {
            SampleId = sampleId;
            MarkerPath = markerPath;
            TotalMs = totalMs;
            SelfMs = selfMs;
            Calls = calls;
        }
        public string SampleId { get; }
        public IReadOnlyList<string> MarkerPath { get; }
        public double TotalMs { get; }
        public double SelfMs { get; }
        public int Calls { get; }
    }

    [Serializable]
    public sealed class SampleTimeResult
    {
        public SampleTimeResult(string sessionId, int frameIndex, ThreadIdentity thread, string sampleId,
            string markerName, IReadOnlyList<string> markerPath, double totalMs, double selfMs, int calls,
            double childrenTotalMs, IReadOnlyList<SampleSummary> ancestors, IReadOnlyList<SampleSummary> children,
            IReadOnlyList<SampleSummary> occurrences)
        {
            SessionId = sessionId; FrameIndex = frameIndex; Thread = thread; SampleId = sampleId;
            MarkerName = markerName; MarkerPath = markerPath; TotalMs = totalMs; SelfMs = selfMs; Calls = calls;
            ChildrenTotalMs = childrenTotalMs; Ancestors = ancestors; Children = children; Occurrences = occurrences;
        }
        public string SessionId { get; }
        public int FrameIndex { get; }
        public ThreadIdentity Thread { get; }
        public string SampleId { get; }
        public string MarkerName { get; }
        public IReadOnlyList<string> MarkerPath { get; }
        public double TotalMs { get; }
        public double SelfMs { get; }
        public int Calls { get; }
        public double ChildrenTotalMs { get; }
        public IReadOnlyList<SampleSummary> Ancestors { get; }
        public IReadOnlyList<SampleSummary> Children { get; }
        public IReadOnlyList<SampleSummary> Occurrences { get; }
    }

    [Serializable]
    public sealed class BottomUpResult
    {
        public BottomUpResult(string sessionId, int frameIndex, ThreadIdentity thread, string bottomUpId,
            string markerName, IReadOnlyList<IReadOnlyList<string>> markerPaths, double totalMs, double selfMs,
            int calls, double occurrenceTotalMs, IReadOnlyList<SelfTimeRow> callers,
            IReadOnlyList<SampleSummary> occurrences)
        {
            SessionId = sessionId; FrameIndex = frameIndex; Thread = thread; BottomUpId = bottomUpId;
            MarkerName = markerName; MarkerPaths = markerPaths; TotalMs = totalMs; SelfMs = selfMs; Calls = calls;
            OccurrenceTotalMs = occurrenceTotalMs; Callers = callers; Occurrences = occurrences;
        }
        public string SessionId { get; }
        public int FrameIndex { get; }
        public ThreadIdentity Thread { get; }
        public string BottomUpId { get; }
        public string MarkerName { get; }
        public IReadOnlyList<IReadOnlyList<string>> MarkerPaths { get; }
        public double TotalMs { get; }
        public double SelfMs { get; }
        public int Calls { get; }
        public double OccurrenceTotalMs { get; }
        public IReadOnlyList<SelfTimeRow> Callers { get; }
        public IReadOnlyList<SampleSummary> Occurrences { get; }
    }

    [Serializable]
    public sealed class MarkerTimeResult
    {
        public MarkerTimeResult(string sessionId, int frameIndex, ThreadIdentity thread, string markerSampleId,
            IReadOnlyList<string> markerPath, double totalMs, double selfMs, int calls)
        {
            SessionId = sessionId; FrameIndex = frameIndex; Thread = thread; MarkerSampleId = markerSampleId;
            MarkerPath = markerPath; TotalMs = totalMs; SelfMs = selfMs; Calls = calls;
        }
        public string SessionId { get; }
        public int FrameIndex { get; }
        public ThreadIdentity Thread { get; }
        public string MarkerSampleId { get; }
        public IReadOnlyList<string> MarkerPath { get; }
        public double TotalMs { get; }
        public double SelfMs { get; }
        public int Calls { get; }
    }

    [Serializable]
    public sealed class InvestigationLinkedIds
    {
        public InvestigationLinkedIds(string bottomUpId, string markerSampleId)
        {
            BottomUpId = bottomUpId;
            MarkerSampleId = markerSampleId;
        }
        public string BottomUpId { get; }
        public string MarkerSampleId { get; }
    }

    [Serializable]
    public sealed class InvestigationLeafSource
    {
        public InvestigationLeafSource(string schema, SelfTimeRow result)
        {
            Schema = schema;
            Result = result;
        }
        public string Schema { get; }
        public SelfTimeRow Result { get; }
    }

    [Serializable]
    public sealed class InvestigationBottomUpSource
    {
        public InvestigationBottomUpSource(string schema, BottomUpResult result)
        {
            Schema = schema;
            Result = result;
        }
        public string Schema { get; }
        public BottomUpResult Result { get; }
    }

    [Serializable]
    public sealed class InvestigationMarkerSource
    {
        public InvestigationMarkerSource(string schema, MarkerTimeResult result)
        {
            Schema = schema;
            Result = result;
        }
        public string Schema { get; }
        public MarkerTimeResult Result { get; }
    }

    [Serializable]
    public sealed class InvestigationReconciliation
    {
        public InvestigationReconciliation(double absoluteToleranceMs, double relativeTolerancePercent,
            double leafSelfMs, double bottomUpSelfMs, double callerOccurrenceTotalMs, double markerTotalMs,
            bool leafMatchesBottomUp, bool callersWithinMarker)
        {
            AbsoluteToleranceMs = absoluteToleranceMs;
            RelativeTolerancePercent = relativeTolerancePercent;
            LeafSelfMs = leafSelfMs;
            BottomUpSelfMs = bottomUpSelfMs;
            CallerOccurrenceTotalMs = callerOccurrenceTotalMs;
            MarkerTotalMs = markerTotalMs;
            LeafMatchesBottomUp = leafMatchesBottomUp;
            CallersWithinMarker = callersWithinMarker;
        }
        public double AbsoluteToleranceMs { get; }
        public double RelativeTolerancePercent { get; }
        public double LeafSelfMs { get; }
        public double BottomUpSelfMs { get; }
        public double CallerOccurrenceTotalMs { get; }
        public double MarkerTotalMs { get; }
        public bool LeafMatchesBottomUp { get; }
        public bool CallersWithinMarker { get; }
    }

    [Serializable]
    public sealed class ProfilerInvestigationResult
    {
        public ProfilerInvestigationResult(string sessionId, int frameIndex, ThreadIdentity thread,
            IReadOnlyList<string> markerPath, InvestigationLinkedIds linkedIds,
            InvestigationLeafSource leaf, InvestigationBottomUpSource bottomUp,
            InvestigationMarkerSource marker, InvestigationReconciliation reconciliation)
        {
            SessionId = sessionId;
            FrameIndex = frameIndex;
            Thread = thread;
            MarkerPath = markerPath;
            LinkedIds = linkedIds;
            Leaf = leaf;
            BottomUp = bottomUp;
            Marker = marker;
            Reconciliation = reconciliation;
        }
        public string SessionId { get; }
        public int FrameIndex { get; }
        public ThreadIdentity Thread { get; }
        public IReadOnlyList<string> MarkerPath { get; }
        public InvestigationLinkedIds LinkedIds { get; }
        public InvestigationLeafSource Leaf { get; }
        public InvestigationBottomUpSource BottomUp { get; }
        public InvestigationMarkerSource Marker { get; }
        public InvestigationReconciliation Reconciliation { get; }
    }
}
