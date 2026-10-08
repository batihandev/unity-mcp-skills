using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BatihanDev.UnityCliCommands.Profiler;
using BatihanDev.UnityCliCommands.Profiler.Internal;
using NUnit.Framework;

namespace BatihanDev.UnityCliCommands.Tests.Profiler
{
    public sealed class ProfilerSessionContractTests
    {
        [Test]
        public void CurrentRefusesMutableHistoryAndRegistersStoppedValidFrames()
        {
            var adapter = new FakeProfilerDataAdapter { IsRecording = true, Frames = Frames(4, 12d) };
            var store = new ProfilerSessionStore(adapter, new FakePersistence());

            Assert.That(store.RegisterCurrent().Error.Code, Is.EqualTo("PROFILER_HISTORY_MUTABLE"));

            adapter.IsRecording = false;
            var registered = store.RegisterCurrent();

            Assert.That(registered.Ok, Is.True);
            Assert.That(registered.Result.Source, Is.EqualTo("memory"));
            Assert.That(registered.Result.CapturePath, Is.Null);
            Assert.That(registered.Result.FirstFrameIndex, Is.EqualTo(4));
            Assert.That(registered.Result.LastFrameIndex, Is.EqualTo(4));
            Assert.That(registered.Result.FrameCount, Is.EqualTo(1));
            Assert.That(registered.Result.HistoryEpoch, Is.GreaterThan(0));
            Assert.That(registered.Result.Fingerprint, Has.Length.GreaterThan(30));
        }

        [Test]
        public void FrameTotalValidatesSessionFrameLimitAndInvalidatesOnHistoryEvent()
        {
            var adapter = new FakeProfilerDataAdapter { Frames = Frames(7, 20d) };
            var store = new ProfilerSessionStore(adapter, new FakePersistence());
            var session = store.RegisterCurrent().Result;

            var result = store.FrameTotal(session.SessionId, 7, 16d, null, null, null);

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.Rows[1].MarkerPath, Is.EqualTo(new[] { "Root", "UnityCliProfilerFixture.Leaf" }));
            Assert.That(result.Result.Rows[1].TotalMs, Is.EqualTo(10d));
            Assert.That(result.Result.TargetMs, Is.EqualTo(16d));
            Assert.That(result.Result.FrameTimeMs, Is.EqualTo(20d));
            Assert.That(result.Result.OverBudget, Is.True);
            Assert.That(result.Result.AvailableRowCount, Is.EqualTo(2));
            Assert.That(store.FrameTotal(session.SessionId, 8, 16d, null, null, null).Error.Code,
                Is.EqualTo("FRAME_NOT_FOUND"));
            Assert.That(store.FrameTotal(session.SessionId, 7, 16d, null, null, 0).Error.Code,
                Is.EqualTo("LIMIT_INVALID"));
            Assert.That(store.FrameTotal(session.SessionId, 7, 16d, null, null, 201).Error.Code,
                Is.EqualTo("LIMIT_INVALID"));

            adapter.SignalHistoryChanged();

            Assert.That(store.FrameTotal(session.SessionId, 7, 16d, null, null, null).Error.Code,
                Is.EqualTo("SESSION_EXPIRED"));
        }

        [Test]
        public void FingerprintChangeAndReloadedPersistenceCannotReviveStaleHistory()
        {
            var persistence = new FakePersistence();
            var adapter = new FakeProfilerDataAdapter { Frames = Frames(3, 8d) };
            var first = new ProfilerSessionStore(adapter, persistence);
            var session = first.RegisterCurrent().Result;

            adapter.Frames = Frames(3, 9d);
            var reloaded = new ProfilerSessionStore(adapter, persistence);

            Assert.That(reloaded.FrameTotal(session.SessionId, 3, 16d, null, null, null).Error.Code,
                Is.EqualTo("SESSION_EXPIRED"));
        }

        [Test]
        public void UnrepresentableProfilerValuesReturnTypedUnavailableData()
        {
            var adapter = new FakeProfilerDataAdapter
            {
                Frames = Frames(1, 5d),
                ReadFailure = new ProfilerDataUnavailableException("overflow")
            };
            var store = new ProfilerSessionStore(adapter, new FakePersistence());

            Assert.That(store.RegisterCurrent().Error.Code, Is.EqualTo("PROFILER_DATA_UNAVAILABLE"));

            adapter.LoadSucceeds = true;
            Assert.That(store.OpenCapture("capture.raw", "Captures/capture.raw").Error.Code,
                Is.EqualTo("PROFILER_DATA_UNAVAILABLE"));
        }

        [Test]
        public void LoadedCaptureWithUnsupportedTraversalExpiresOriginalSessionWithoutPartialResult()
        {
            var adapter = new FakeProfilerDataAdapter { Frames = Frames(1, 5d) };
            var store = new ProfilerSessionStore(adapter, new FakePersistence());
            var original = store.RegisterCurrent().Result;
            adapter.LoadSucceeds = true;
            adapter.ReadFailure = new NotSupportedException("unsupported capture traversal");

            var opened = store.OpenCapture("capture.raw", "Captures/capture.raw");

            Assert.That(opened.Ok, Is.False);
            Assert.That(opened.Result, Is.Null);
            Assert.That(opened.Error.Code, Is.EqualTo("CAPTURE_UNSUPPORTED"));
            adapter.ReadFailure = null;
            Assert.That(store.FrameTotal(original.SessionId, 1, 16d, null, null, null).Error.Code,
                Is.EqualTo("SESSION_EXPIRED"));
        }

        [Test]
        public void TimingCommandsRejectWrongFramesThreadsStaleAndWrongKinds()
        {
            var adapter = new FakeProfilerDataAdapter { Frames = Frames(7, 20d) };
            var store = new ProfilerSessionStore(adapter, new FakePersistence());
            var session = store.RegisterCurrent().Result;

            Assert.That(store.TimeRange(session.SessionId, 7, 7, 16d).Result.SessionId,
                Is.EqualTo(session.SessionId));
            Assert.That(store.TimeRange(session.SessionId, 8, 8, 16d).Error.Code,
                Is.EqualTo("FRAME_RANGE_INVALID"));
            Assert.That(store.FrameSelf(session.SessionId, 7, null, "Missing", null).Error.Code,
                Is.EqualTo("THREAD_NOT_FOUND"));

            var row = store.FrameSelf(session.SessionId, 7, null, null, null).Result.Rows[0];
            Assert.That(store.BottomUp(session.SessionId, 7, row.BottomUpId, null, null).Ok, Is.True);
            Assert.That(store.Sample(session.SessionId, 7, row.BottomUpId, null, null).Error.Code,
                Is.EqualTo("SAMPLE_ID_KIND_MISMATCH"));
            adapter.SignalHistoryChanged();
            Assert.That(store.BottomUp(session.SessionId, 7, row.BottomUpId, null, null).Error.Code,
                Is.EqualTo("SESSION_EXPIRED"));
        }

        [Test]
        public void GcCommandsKeepSessionFrameThreadAndIdentityFailuresTyped()
        {
            var adapter = new FakeProfilerDataAdapter { Frames = Frames(7, 20d) };
            var store = new ProfilerSessionStore(adapter, new FakePersistence());
            var session = store.RegisterCurrent().Result;

            Assert.That(store.GcOverall(session.SessionId, 8192).Result.SessionId,
                Is.EqualTo(session.SessionId));
            Assert.That(store.GcFrame(session.SessionId, 7, null, null, 8192).Result.FrameIndex,
                Is.EqualTo(7));
            Assert.That(store.GcRange(session.SessionId, 7, 7, 8192).Result.FrameCount, Is.EqualTo(1));
            Assert.That(store.GcFrame(session.SessionId, 8, null, null, 8192).Error.Code,
                Is.EqualTo("FRAME_NOT_FOUND"));
            Assert.That(store.GcFrame(session.SessionId, 7, null, "Missing", 8192).Error.Code,
                Is.EqualTo("THREAD_NOT_FOUND"));

            var normalId = store.FrameTotal(session.SessionId, 7, 16d, null, null, null)
                .Result.Rows.Single(row => row.MarkerPath.Last() == "UnityCliProfilerFixture.Leaf").SampleId;
            Assert.That(store.GcSample(session.SessionId, 7, normalId, null, null).Ok, Is.True);
            Assert.That(store.GcMarkerPath(session.SessionId, 7,
                new[] { "Root", "UnityCliProfilerFixture.Leaf" }, null, null).Ok, Is.True);

            adapter.SignalHistoryChanged();
            Assert.That(store.GcSample(session.SessionId, 7, normalId, null, null).Error.Code,
                Is.EqualTo("SESSION_EXPIRED"));
        }

        [Test]
        public void FailedAndSuccessfulLoadsInvalidateTheOriginalSessionBeforeReplacement()
        {
            var adapter = new FakeProfilerDataAdapter { Frames = Frames(1, 5d) };
            var store = new ProfilerSessionStore(adapter, new FakePersistence());
            var original = store.RegisterCurrent().Result;

            adapter.LoadSucceeds = false;
            Assert.That(store.OpenCapture("capture.raw", "Captures/capture.raw").Error.Code,
                Is.EqualTo("CAPTURE_INVALID"));
            Assert.That(store.FrameTotal(original.SessionId, 1, 16d, null, null, null).Error.Code,
                Is.EqualTo("SESSION_EXPIRED"));

            adapter.LoadSucceeds = true;
            adapter.FramesAfterLoad = Frames(11, 24d);
            var replacement = store.OpenCapture("capture.data", "Captures/capture.data");

            Assert.That(replacement.Ok, Is.True);
            Assert.That(replacement.Result.Source, Is.EqualTo("capture"));
            Assert.That(replacement.Result.CapturePath, Is.EqualTo("Captures/capture.data"));
            Assert.That(replacement.Result.SessionId, Is.Not.EqualTo(original.SessionId));
            Assert.That(store.FrameTotal(original.SessionId, 11, 16d, null, null, null).Error.Code,
                Is.EqualTo("SESSION_EXPIRED"));
            Assert.That(store.FrameTotal("ps1-999-abcdefghijklmnop", 11, 16d, null, null, null).Error.Code,
                Is.EqualTo("SESSION_MISMATCH"));
        }

        [Test]
        public void CaptureDiscoveryReturnsEverySupportedFileInOrdinalOrderWithoutFollowingLinks()
        {
            var root = Path.Combine(Path.GetTempPath(), "UnityCliProfilerDiscovery-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "Captures", "Nested"));
            File.WriteAllText(Path.Combine(root, "Captures", "z.raw"), "raw");
            File.WriteAllText(Path.Combine(root, "Captures", "Nested", "a.data"), "data");
            File.WriteAllText(Path.Combine(root, "Captures", "ignored.txt"), "text");
            try
            {
                var discovered = ProfilerCommands.DiscoverCaptureCandidates(root);
                Assert.That(discovered.Ok, Is.True);
                Assert.That(discovered.Result, Is.EqualTo(new[]
                {
                    "Captures/Nested/a.data", "Captures/z.raw"
                }));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void CaptureDiscoveryReturnsTypedAccessAndIoFailures()
        {
            var access = ProfilerCommands.DiscoverCaptureCandidates("project",
                _ => throw new UnauthorizedAccessException());
            var io = ProfilerCommands.DiscoverCaptureCandidates("project",
                _ => throw new IOException());

            Assert.That(access.Error.Code, Is.EqualTo("PATH_ACCESS_DENIED"));
            Assert.That(io.Error.Code, Is.EqualTo("PATH_IO_ERROR"));
            Assert.That(access.Error.Message, Does.Not.Contain("project"));
        }

        [Test]
        public void UnreadableCaptureDoesNotReachLoadOrInvalidateTheCurrentSession()
        {
            var root = Path.Combine(Path.GetTempPath(), "UnityCliProfilerUnreadable-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "capture.raw"), "capture");
            try
            {
                var adapter = new FakeProfilerDataAdapter { Frames = Frames(1, 5d) };
                var store = new ProfilerSessionStore(adapter, new FakePersistence());
                var original = store.RegisterCurrent().Result;

                var result = ProfilerCommands.OpenExplicitCapture("capture.raw", root, store,
                    _ => throw new UnauthorizedAccessException());

                Assert.That(result.Error.Code, Is.EqualTo("PATH_ACCESS_DENIED"));
                Assert.That(adapter.LoadCalls, Is.Zero);
                Assert.That(store.FrameTotal(original.SessionId, 1, 16d, null, null, null).Ok, Is.True);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        private static IReadOnlyList<ProfilerFrameRecord> Frames(int frameIndex, double frameMs)
        {
            var normal = new[]
            {
                Sample(1, null, "Root", frameMs, frameMs - 10d, 10, new[] { 101 }),
                Sample(2, 1, "UnityCliProfilerFixture.Leaf", 10d, 4d, 20, new[] { 102 })
            };
            var raw = new[]
            {
                Sample(101, null, "Root", frameMs, frameMs - 10d, 10),
                Sample(102, 101, "UnityCliProfilerFixture.Leaf", 10d, 4d, 20)
            };
            var inverted = new[]
            {
                Sample(201, null, "Inverted Root", frameMs, 0d, -1),
                Sample(202, 201, "UnityCliProfilerFixture.Leaf", 10d, 4d, 20, new[] { 102 }),
                Sample(203, 202, "Root", 10d, 0d, 10, new[] { 101 })
            };
            return new[]
            {
                new ProfilerFrameRecord(frameIndex, frameIndex * 1000000L, (long)(frameMs * 1000000d),
                    new[]
                    {
                        new ProfilerThreadRecord(0, 100, "", "Main Thread", true,
                            normal, raw, inverted)
                    })
            };
        }

        private static ProfilerSampleRecord Sample(
            int id, int? parent, string name, double totalMs, double selfMs, int markerId,
            IReadOnlyList<int> rawSampleIndices = null)
        {
            return new ProfilerSampleRecord(id, parent, name, 0, (long)(totalMs * 1000000d),
                (long)(selfMs * 1000000d), 1, 0, 0, false, Array.Empty<long>(), markerId, rawSampleIndices);
        }

        private sealed class FakeProfilerDataAdapter : IProfilerDataAdapter
        {
            public bool IsRecording { get; set; }
            public bool LoadSucceeds { get; set; }
            public IReadOnlyList<ProfilerFrameRecord> Frames { get; set; } = Array.Empty<ProfilerFrameRecord>();
            public IReadOnlyList<ProfilerFrameRecord> FramesAfterLoad { get; set; }
            public Exception ReadFailure { get; set; }
            public int LoadCalls { get; private set; }
            public event Action HistoryChanged;

            public IReadOnlyList<ProfilerFrameRecord> ReadAllFrames()
            {
                if (ReadFailure != null)
                    throw ReadFailure;
                return Frames;
            }

            public bool LoadProfile(string capturePath)
            {
                LoadCalls++;
                if (LoadSucceeds && FramesAfterLoad != null)
                    Frames = FramesAfterLoad;
                return LoadSucceeds;
            }

            public void SignalHistoryChanged() => HistoryChanged?.Invoke();
        }

        private sealed class FakePersistence : IProfilerSessionPersistence
        {
            private ProfilerSessionState _state = new ProfilerSessionState();

            public ProfilerSessionState Load() => _state;

            public void Save(ProfilerSessionState state) => _state = state;
        }
    }
}
