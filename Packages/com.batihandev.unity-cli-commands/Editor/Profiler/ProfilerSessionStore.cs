using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using BatihanDev.UnityCliCommands.Foundation;
using BatihanDev.UnityCliCommands.Profiler.Analysis;
using BatihanDev.UnityCliCommands.Profiler.Internal;
using BatihanDev.UnityCliCommands.Profiler.Schemas;
using UnityEditor;

namespace BatihanDev.UnityCliCommands.Profiler
{
    internal sealed class ProfilerSessionStore
    {
        private readonly object _gate = new object();
        private readonly IProfilerDataAdapter _adapter;
        private readonly IProfilerSessionPersistence _persistence;
        private ProfilerSessionState _state;

        public ProfilerSessionStore(IProfilerDataAdapter adapter, IProfilerSessionPersistence persistence)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            _state = persistence.Load() ?? new ProfilerSessionState();
            _adapter.HistoryChanged += Invalidate;
        }

        public CommandResult<ProfilerSessionResult> RegisterCurrent()
        {
            lock (_gate)
            {
                if (_adapter.IsRecording)
                    return Failure<ProfilerSessionResult>(ProfilerErrorCode.ProfilerHistoryMutable);

                IReadOnlyList<ProfilerFrameRecord> frames;
                try
                {
                    frames = _adapter.ReadAllFrames();
                }
                catch (ProfilerDataUnavailableException)
                {
                    return Failure<ProfilerSessionResult>(ProfilerErrorCode.ProfilerDataUnavailable);
                }
                catch
                {
                    return Failure<ProfilerSessionResult>(ProfilerErrorCode.ProfilerDataUnavailable);
                }

                if (frames == null || frames.Count == 0)
                    return Failure<ProfilerSessionResult>(ProfilerErrorCode.SessionNotFound);

                AdvanceEpoch();
                return Register(frames, "memory", null);
            }
        }

        public CommandResult<ProfilerSessionResult> OpenCapture(string capturePath, string projectRelativePath)
        {
            lock (_gate)
            {
                if (_adapter.IsRecording)
                    return Failure<ProfilerSessionResult>(ProfilerErrorCode.ProfilerHistoryMutable);

                AdvanceEpoch();
                bool loaded;
                try
                {
                    loaded = _adapter.LoadProfile(capturePath);
                }
                catch
                {
                    return Failure<ProfilerSessionResult>(ProfilerErrorCode.CaptureInvalid);
                }

                if (!loaded)
                    return Failure<ProfilerSessionResult>(ProfilerErrorCode.CaptureInvalid);

                IReadOnlyList<ProfilerFrameRecord> frames;
                try
                {
                    frames = _adapter.ReadAllFrames();
                }
                catch (ProfilerDataUnavailableException)
                {
                    return Failure<ProfilerSessionResult>(ProfilerErrorCode.ProfilerDataUnavailable);
                }
                catch
                {
                    return Failure<ProfilerSessionResult>(ProfilerErrorCode.CaptureUnsupported);
                }

                if (frames == null || frames.Count == 0)
                    return Failure<ProfilerSessionResult>(ProfilerErrorCode.CaptureInvalid);
                return Register(frames, "capture", projectRelativePath);
            }
        }

        public CommandResult<FrameTotalResult> FrameTotal(string sessionId, int frameIndex, double targetMs,
            int? threadIndex, string threadName, int? limit)
        {
            lock (_gate)
            {
                var session = ValidateSession(sessionId);
                if (!session.Ok)
                    return CommandResult<FrameTotalResult>.Failure(ProfilerSchema.FrameTotal, session.Error);
                if (limit.HasValue && (limit.Value < 1 || limit.Value > 200))
                    return Failure<FrameTotalResult>(ProfilerErrorCode.LimitInvalid, ProfilerSchema.FrameTotal);

                var frame = session.Result.FirstOrDefault(value => value.FrameIndex == frameIndex);
                if (frame == null)
                    return Failure<FrameTotalResult>(ProfilerErrorCode.FrameNotFound, ProfilerSchema.FrameTotal);
                return ProfilerAnalysis.FrameTotal(frame, targetMs, threadIndex, threadName, limit ?? 20,
                    _state.HistoryEpoch, _state.SessionId);
            }
        }

        public CommandResult<TimeRangeResult> TimeRange(string sessionId, int firstFrameIndex, int lastFrameIndex,
            double targetMs)
        {
            lock (_gate)
            {
                var session = ValidateSession(sessionId);
                if (!session.Ok)
                    return CommandResult<TimeRangeResult>.Failure(ProfilerSchema.TimeRange, session.Error);
                return ProfilerAnalysis.SummarizeTimeRange(session.Result, firstFrameIndex, lastFrameIndex,
                    targetMs, _state.SessionId);
            }
        }

        public CommandResult<FrameSelfResult> FrameSelf(string sessionId, int frameIndex, int? threadIndex,
            string threadName, int? limit)
        {
            lock (_gate)
            {
                var context = ResolveFrameThread(sessionId, frameIndex, threadIndex, threadName,
                    ProfilerSchema.FrameSelf);
                if (!context.Ok)
                    return CommandResult<FrameSelfResult>.Failure(ProfilerSchema.FrameSelf, context.Error);
                if (limit.HasValue && (limit.Value < 1 || limit.Value > 200))
                    return Failure<FrameSelfResult>(ProfilerErrorCode.LimitInvalid, ProfilerSchema.FrameSelf);
                return ProfilerAnalysis.FrameSelf(context.Result.Frame, threadIndex, threadName, limit ?? 20,
                    _state.HistoryEpoch, _state.SessionId);
            }
        }

        public CommandResult<SampleTimeResult> Sample(string sessionId, int frameIndex, string sampleId,
            int? threadIndex, string threadName)
        {
            lock (_gate)
            {
                var context = ResolveFrameThread(sessionId, frameIndex, threadIndex, threadName,
                    ProfilerSchema.SampleTime);
                if (!context.Ok)
                    return CommandResult<SampleTimeResult>.Failure(ProfilerSchema.SampleTime, context.Error);
                return ProfilerAnalysis.Sample(context.Result.Frame, context.Result.Thread, sampleId,
                    _state.HistoryEpoch, _state.SessionId);
            }
        }

        public CommandResult<BottomUpResult> BottomUp(string sessionId, int frameIndex, string bottomUpId,
            int? threadIndex, string threadName)
        {
            lock (_gate)
            {
                var context = ResolveFrameThread(sessionId, frameIndex, threadIndex, threadName,
                    ProfilerSchema.BottomUp);
                if (!context.Ok)
                    return CommandResult<BottomUpResult>.Failure(ProfilerSchema.BottomUp, context.Error);
                return ProfilerAnalysis.BottomUp(context.Result.Frame, context.Result.Thread, bottomUpId,
                    _state.HistoryEpoch, _state.SessionId);
            }
        }

        public CommandResult<MarkerTimeResult> MarkerPath(string sessionId, int frameIndex,
            IReadOnlyList<string> markerPath, int? threadIndex, string threadName)
        {
            lock (_gate)
            {
                var context = ResolveFrameThread(sessionId, frameIndex, threadIndex, threadName,
                    ProfilerSchema.MarkerTime);
                if (!context.Ok)
                    return CommandResult<MarkerTimeResult>.Failure(ProfilerSchema.MarkerTime, context.Error);
                if (markerPath == null || markerPath.Count == 0 || markerPath.Any(string.IsNullOrEmpty))
                    return Failure<MarkerTimeResult>(ProfilerErrorCode.MarkerPathNotFound, ProfilerSchema.MarkerTime);
                return ProfilerAnalysis.MarkerPath(context.Result.Frame, context.Result.Thread, markerPath,
                    _state.HistoryEpoch, _state.SessionId);
            }
        }

        public CommandResult<RelatedTimeResult> Related(string sessionId, int frameIndex, string sourceSampleId,
            int? sourceThreadIndex, string sourceThreadName, int? targetThreadIndex, string targetThreadName)
        {
            lock (_gate)
            {
                var source = ResolveFrameThread(sessionId, frameIndex, sourceThreadIndex, sourceThreadName,
                    ProfilerSchema.RelatedTime);
                if (!source.Ok)
                    return CommandResult<RelatedTimeResult>.Failure(ProfilerSchema.RelatedTime, source.Error);
                var target = ProfilerAnalysis.SelectThread(source.Result.Frame, targetThreadIndex, targetThreadName);
                if (!target.Ok)
                    return CommandResult<RelatedTimeResult>.Failure(ProfilerSchema.RelatedTime, target.Error);
                return ProfilerAnalysis.Related(source.Result.Frame, source.Result.Thread, sourceSampleId,
                    target.Result, _state.HistoryEpoch, _state.SessionId);
            }
        }

        public CommandResult<GcOverallResult> GcOverall(string sessionId, long thresholdBytes)
        {
            lock (_gate)
            {
                var validated = ValidateSession(sessionId);
                if (!validated.Ok)
                    return CommandResult<GcOverallResult>.Failure(ProfilerSchema.GcOverall, validated.Error);
                return ProfilerAnalysis.OverallGc(validated.Result, thresholdBytes,
                    _state.HistoryEpoch, sessionId);
            }
        }

        public CommandResult<GcFrameResult> GcFrame(string sessionId, int frameIndex, int? threadIndex,
            string threadName, long thresholdBytes)
        {
            lock (_gate)
            {
                var context = ResolveFrameThread(sessionId, frameIndex, threadIndex, threadName,
                    ProfilerSchema.GcFrame);
                if (!context.Ok)
                    return CommandResult<GcFrameResult>.Failure(ProfilerSchema.GcFrame, context.Error);
                return ProfilerAnalysis.FrameGc(context.Result.Frame, threadIndex, threadName, thresholdBytes,
                    _state.HistoryEpoch, sessionId);
            }
        }

        public CommandResult<GcRangeResult> GcRange(string sessionId, int firstFrameIndex,
            int lastFrameIndex, long thresholdBytes)
        {
            lock (_gate)
            {
                var validated = ValidateSession(sessionId);
                if (!validated.Ok)
                    return CommandResult<GcRangeResult>.Failure(ProfilerSchema.GcRange, validated.Error);
                return ProfilerAnalysis.RangeGc(validated.Result, firstFrameIndex, lastFrameIndex,
                    thresholdBytes, sessionId);
            }
        }

        public CommandResult<GcAttributionResult> GcSample(string sessionId, int frameIndex, string sampleId,
            int? threadIndex, string threadName)
        {
            lock (_gate)
            {
                var context = ResolveFrameThread(sessionId, frameIndex, threadIndex, threadName,
                    ProfilerSchema.GcSample);
                if (!context.Ok)
                    return CommandResult<GcAttributionResult>.Failure(ProfilerSchema.GcSample, context.Error);
                return ProfilerAnalysis.SampleGc(context.Result.Frame, context.Result.Thread, sampleId,
                    _state.HistoryEpoch, sessionId);
            }
        }

        public CommandResult<GcAttributionResult> GcMarkerPath(string sessionId, int frameIndex,
            IReadOnlyList<string> markerPath, int? threadIndex, string threadName)
        {
            lock (_gate)
            {
                var context = ResolveFrameThread(sessionId, frameIndex, threadIndex, threadName,
                    ProfilerSchema.GcMarker);
                if (!context.Ok)
                    return CommandResult<GcAttributionResult>.Failure(ProfilerSchema.GcMarker, context.Error);
                if (markerPath == null || markerPath.Count == 0)
                    return Failure<GcAttributionResult>(ProfilerErrorCode.MarkerPathNotFound,
                        ProfilerSchema.GcMarker);
                return ProfilerAnalysis.MarkerGc(context.Result.Frame, context.Result.Thread, markerPath,
                    _state.HistoryEpoch, sessionId);
            }
        }

        private CommandResult<FrameThreadContext> ResolveFrameThread(string sessionId, int frameIndex,
            int? threadIndex, string threadName, string schema)
        {
            var session = ValidateSession(sessionId);
            if (!session.Ok)
                return CommandResult<FrameThreadContext>.Failure(schema, session.Error);
            var frame = session.Result.FirstOrDefault(value => value.FrameIndex == frameIndex);
            if (frame == null)
                return Failure<FrameThreadContext>(ProfilerErrorCode.FrameNotFound, schema);
            var thread = ProfilerAnalysis.SelectThread(frame, threadIndex, threadName);
            if (!thread.Ok)
                return CommandResult<FrameThreadContext>.Failure(schema, thread.Error);
            return CommandResult<FrameThreadContext>.Success(schema, new FrameThreadContext(frame, thread.Result));
        }

        private CommandResult<IReadOnlyList<ProfilerFrameRecord>> ValidateSession(string sessionId)
        {
            if (string.IsNullOrEmpty(_state.SessionId))
                return Failure<IReadOnlyList<ProfilerFrameRecord>>(ProfilerErrorCode.SessionExpired);
            if (!string.Equals(_state.SessionId, sessionId, StringComparison.Ordinal))
            {
                return TryReadSessionEpoch(sessionId, out var requestedEpoch) &&
                       requestedEpoch < _state.HistoryEpoch
                    ? Failure<IReadOnlyList<ProfilerFrameRecord>>(ProfilerErrorCode.SessionExpired)
                    : Failure<IReadOnlyList<ProfilerFrameRecord>>(ProfilerErrorCode.SessionMismatch);
            }

            IReadOnlyList<ProfilerFrameRecord> frames;
            try
            {
                frames = _adapter.ReadAllFrames();
            }
            catch
            {
                Invalidate();
                return Failure<IReadOnlyList<ProfilerFrameRecord>>(ProfilerErrorCode.SessionExpired);
            }

            if (!string.Equals(ProfilerFingerprint.Compute(frames), _state.Fingerprint, StringComparison.Ordinal))
            {
                Invalidate();
                return Failure<IReadOnlyList<ProfilerFrameRecord>>(ProfilerErrorCode.SessionExpired);
            }
            return CommandResult<IReadOnlyList<ProfilerFrameRecord>>.Success(ProfilerSchema.Session, frames);
        }

        private CommandResult<ProfilerSessionResult> Register(
            IReadOnlyList<ProfilerFrameRecord> frames, string source, string capturePath)
        {
            var ordered = frames.OrderBy(value => value.FrameIndex).ToList();
            var fingerprint = ProfilerFingerprint.Compute(ordered);
            var sessionId = "ps1-" + _state.HistoryEpoch + "-" + fingerprint.Substring(0, 16).ToLowerInvariant();
            _state.SessionId = sessionId;
            _state.Fingerprint = fingerprint;
            _persistence.Save(_state);
            return CommandResult<ProfilerSessionResult>.Success(ProfilerSchema.Session,
                new ProfilerSessionResult(sessionId, source, capturePath, ordered[0].FrameIndex,
                    ordered[ordered.Count - 1].FrameIndex, ordered.Count, _state.HistoryEpoch, fingerprint));
        }

        private void Invalidate()
        {
            lock (_gate)
            {
                AdvanceEpoch();
            }
        }

        private void AdvanceEpoch()
        {
            _state.HistoryEpoch = _state.HistoryEpoch == long.MaxValue ? 1 : _state.HistoryEpoch + 1;
            _state.SessionId = null;
            _state.Fingerprint = null;
            _persistence.Save(_state);
        }

        private static CommandResult<T> Failure<T>(string code, string schema = ProfilerSchema.Session)
        {
            return CommandResult<T>.Failure(schema, code, "Profiler data does not satisfy the requested operation.");
        }

        private static bool TryReadSessionEpoch(string sessionId, out long epoch)
        {
            epoch = 0;
            if (string.IsNullOrEmpty(sessionId) || !sessionId.StartsWith("ps1-", StringComparison.Ordinal))
                return false;
            var separator = sessionId.IndexOf('-', 4);
            if (separator <= 4 || sessionId.Length - separator - 1 != 16 ||
                !long.TryParse(sessionId.Substring(4, separator - 4), NumberStyles.None,
                    CultureInfo.InvariantCulture, out epoch) || epoch <= 0)
                return false;
            for (var index = separator + 1; index < sessionId.Length; index++)
            {
                var character = sessionId[index];
                if ((character < 'a' || character > 'z') && (character < '0' || character > '9') &&
                    character != '_' && character != '-')
                    return false;
            }
            return true;
        }
    }

    internal sealed class FrameThreadContext
    {
        public FrameThreadContext(ProfilerFrameRecord frame, ProfilerThreadRecord thread)
        {
            Frame = frame;
            Thread = thread;
        }
        public ProfilerFrameRecord Frame { get; }
        public ProfilerThreadRecord Thread { get; }
    }

    internal interface IProfilerSessionPersistence
    {
        ProfilerSessionState Load();
        void Save(ProfilerSessionState state);
    }

    internal sealed class ProfilerSessionState
    {
        public long HistoryEpoch { get; set; }
        public string SessionId { get; set; }
        public string Fingerprint { get; set; }
    }

    internal sealed class SessionStateProfilerSessionPersistence : IProfilerSessionPersistence
    {
        private const string EpochKey = "BatihanDev.UnityCliCommands.Profiler.HistoryEpoch";
        private const string SessionKey = "BatihanDev.UnityCliCommands.Profiler.SessionId";
        private const string FingerprintKey = "BatihanDev.UnityCliCommands.Profiler.Fingerprint";

        public ProfilerSessionState Load()
        {
            return new ProfilerSessionState
            {
                HistoryEpoch = ParseEpoch(SessionState.GetString(EpochKey, "0")),
                SessionId = SessionState.GetString(SessionKey, null),
                Fingerprint = SessionState.GetString(FingerprintKey, null)
            };
        }

        public void Save(ProfilerSessionState state)
        {
            SessionState.SetString(EpochKey, state.HistoryEpoch.ToString(CultureInfo.InvariantCulture));
            SessionState.SetString(SessionKey, state.SessionId);
            SessionState.SetString(FingerprintKey, state.Fingerprint);
        }

        private static long ParseEpoch(string value)
        {
            return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var epoch) && epoch >= 0
                ? epoch
                : 0;
        }
    }
}
