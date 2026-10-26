using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Profiler.Analysis;
using BatihanDev.UnityCliCommands.Profiler.Internal;
using BatihanDev.UnityCliCommands.Profiler.Schemas;
using NUnit.Framework;

namespace BatihanDev.UnityCliCommands.Tests.Profiler
{
    public sealed class ProfilerPureContractTests
    {
        [Test]
        public void InclusiveRangeUsesEvenMedianNearestRankP95AndLowestMaximumFrame()
        {
            var result = ProfilerAnalysis.SummarizeTimeRange(
                Frames((7, 10d), (8, 40d), (9, 20d), (10, 40d)), 7, 10, 16d, "session-a");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.FrameCount, Is.EqualTo(4));
            Assert.That(result.Result.SessionId, Is.EqualTo("session-a"));
            Assert.That(result.Result.MinMs, Is.EqualTo(10d));
            Assert.That(result.Result.MedianMs, Is.EqualTo(30d));
            Assert.That(result.Result.P95Ms, Is.EqualTo(40d));
            Assert.That(result.Result.MaxMs, Is.EqualTo(40d));
            Assert.That(result.Result.MaxFrameIndex, Is.EqualTo(8));
            Assert.That(result.Result.OverBudgetFrameCount, Is.EqualTo(3));
            Assert.That(result.Result.OverBudgetPercent, Is.EqualTo(75d));
            Assert.That(result.Result.MaxOverBudgetMs, Is.EqualTo(24d));
            Assert.That(result.Result.MaxBudgetRatio, Is.EqualTo(2.5d));
            Assert.That(result.Result.PercentileMethod, Is.EqualTo("nearest-rank"));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(0d)]
        [TestCase(-1d)]
        public void RangeRejectsEveryNonFiniteOrNonPositiveTargetWithoutPartialResult(double targetMs)
        {
            var result = ProfilerAnalysis.SummarizeTimeRange(
                Frames((1, 1d)), 1, 1, targetMs, "session-a");

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Result, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("TARGET_MS_INVALID"));
        }

        [Test]
        public void RangeRejectsInversionAndIntegerGap()
        {
            Assert.That(ProfilerAnalysis.SummarizeTimeRange(Frames((1, 1d)), 2, 1, 1d, "session-a").Error.Code,
                Is.EqualTo("FRAME_RANGE_INVALID"));
            Assert.That(ProfilerAnalysis.SummarizeTimeRange(Frames((1, 1d), (3, 3d)), 1, 3, 1d, "session-a").Error.Code,
                Is.EqualTo("FRAME_RANGE_GAP"));
        }

        [Test]
        public void RangeRefusesUnrepresentableBudgetArithmeticWithoutPartialResult()
        {
            var result = ProfilerAnalysis.SummarizeTimeRange(
                Frames((1, 10d), (2, 20d)), 1, 2, 1e-320d, "session-a");

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Result, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("PROFILER_DATA_UNAVAILABLE"));
        }

        [Test]
        public void SelfTimeSampleBottomUpAndMarkerPathPreserveKindsAndReconcile()
        {
            var normal = new[]
            {
                Sample(1, null, "Root", 0d, 20d, 2d),
                Sample(2, 1, "Branch", 1d, 12d, 3d),
                Sample(3, 2, "Leaf", 2d, 9d, 9d)
            };
            var raw = new[]
            {
                Sample(10, null, "Root", 0d, 20d, 2d, markerId: 1),
                Sample(11, 10, "Branch", 1d, 12d, 3d, markerId: 2),
                Sample(12, 11, "Leaf", 2d, 9d, 9d, markerId: 3)
            };
            var inverted = new[]
            {
                Sample(20, null, "Inverted Root", 0d, 20d, 0d, markerId: -1),
                Sample(21, 20, "Leaf", 2d, 9d, 9d, markerId: 3),
                Sample(22, 21, "Branch", 1d, 9d, 0d, markerId: 2)
            };
            var thread = new ProfilerThreadRecord(0, 100, "Fixture", "Main Thread", true,
                normal, raw, inverted);
            var frame = Frame(4, 20d, thread);

            var self = ProfilerAnalysis.FrameSelf(frame, null, null, 20, 3, "session-a");
            Assert.That(self.Result.SessionId, Is.EqualTo("session-a"));
            Assert.That(self.Result.Rows[0].MarkerPaths,
                Is.EqualTo(new[] { new[] { "Root", "Branch", "Leaf" } }));
            Assert.That(self.Result.Rows[0].SelfMs, Is.EqualTo(9d));
            Assert.That(ProfilerOpaqueId.Decode(self.Result.Rows[0].BottomUpId, 3, ProfilerViewKind.BottomUp).Ok,
                Is.True);

            var branchId = ProfilerOpaqueId.Encode(
                new ProfilerOpaqueIdPayload(1, 3, ProfilerViewKind.Normal, 4, 0, 2));
            var sample = ProfilerAnalysis.Sample(frame, frame.Threads[0], branchId, 3, "session-a");
            Assert.That(sample.Result.MarkerPath, Is.EqualTo(new[] { "Root", "Branch" }));
            Assert.That(sample.Result.Children[0].MarkerPath, Is.EqualTo(new[] { "Root", "Branch", "Leaf" }));
            Assert.That(sample.Result.SelfMs + sample.Result.ChildrenTotalMs, Is.EqualTo(sample.Result.TotalMs));

            var bottomUp = ProfilerAnalysis.BottomUp(frame, frame.Threads[0], self.Result.Rows[0].BottomUpId,
                3, "session-a");
            Assert.That(bottomUp.Result.MarkerName, Is.EqualTo("Leaf"));
            Assert.That(bottomUp.Result.SelfMs, Is.EqualTo(9d));
            Assert.That(bottomUp.Result.OccurrenceTotalMs, Is.EqualTo(9d));
            Assert.That(bottomUp.Result.Callers[0].MarkerPaths,
                Is.EqualTo(new[] { new[] { "Root", "Branch" } }));

            var marker = ProfilerAnalysis.MarkerPath(frame, frame.Threads[0],
                new[] { "Root", "Branch", "Leaf" }, 3, "session-a");
            var leafId = ProfilerOpaqueId.Encode(
                new ProfilerOpaqueIdPayload(1, 3, ProfilerViewKind.Normal, 4, 0, 3));
            Assert.That(marker.Result.MarkerSampleId, Is.EqualTo(leafId));
            Assert.That(marker.Result.TotalMs, Is.EqualTo(9d));

            Assert.That(ProfilerAnalysis.Sample(frame, frame.Threads[0], self.Result.Rows[0].BottomUpId,
                3, "session-a").Error.Code, Is.EqualTo("SAMPLE_ID_KIND_MISMATCH"));
            Assert.That(ProfilerAnalysis.BottomUp(frame, frame.Threads[0], branchId,
                3, "session-a").Error.Code, Is.EqualTo("SAMPLE_ID_KIND_MISMATCH"));
        }

        [Test]
        public void RelatedRowsUseActualFrameAndThreadIdentitiesAndRealFlowEvidence()
        {
            var source = Sample(10, null, "Source", 10d, 10d, 1d, flowIds: new[] { 42L });
            var worker = Thread("Worker", new[]
            {
                Sample(20, null, "Long", 11d, 8d, 1d),
                Sample(21, null, "Flow", 12d, 5d, 1d, flowIds: new[] { 42L }),
                Sample(22, null, "Boundary", 20d, 2d, 1d)
            }, threadIndex: 2);
            var sourceThread = Thread("Main Thread", new[] { source }, threadIndex: 0);
            var frame = Frame(9, 30d, sourceThread, worker);
            var sourceId = ProfilerOpaqueId.Encode(
                new ProfilerOpaqueIdPayload(1, 7, ProfilerViewKind.Raw, 9, 0, 10));

            var result = ProfilerAnalysis.Related(frame, sourceThread, sourceId, worker, 7, "session-a");

            Assert.That(result.Result.SessionId, Is.EqualTo("session-a"));
            Assert.That(result.Result.FrameIndex, Is.EqualTo(9));
            Assert.That(result.Result.SourceThread.ThreadIndex, Is.EqualTo(0));
            Assert.That(result.Result.TargetThread.ThreadIndex, Is.EqualTo(2));
            Assert.That(result.Result.Rows.Count, Is.EqualTo(2));
            Assert.That(result.Result.Rows[0].MarkerPath, Is.EqualTo(new[] { "Long" }));
            Assert.That(result.Result.Rows[0].OverlapMs, Is.EqualTo(8d));
            Assert.That(result.Result.Rows[0].Relation, Is.EqualTo("temporal-overlap"));
            Assert.That(result.Result.Rows[1].Relation, Is.EqualTo("flow"));
            Assert.That(ProfilerOpaqueId.Decode(result.Result.Rows[0].SampleId, 7, ProfilerViewKind.Raw).Result.FrameIndex,
                Is.EqualTo(9));
        }

        [Test]
        public void IndependentViewsPreserveEveryAggregatePathAndJoinNormalSamplesByExactPath()
        {
            var normal = new[]
            {
                Sample(1, null, "Root", 0d, 20d, 1d, markerId: 10, rawSampleIndices: new[] { 100 }),
                Sample(2, 1, "A", 0d, 10d, 1d, markerId: 20, rawSampleIndices: new[] { 110 }),
                Sample(3, 2, "Leaf", 0d, 8d, 8d, markerId: 30, rawSampleIndices: new[] { 111, 112 }),
                Sample(4, 1, "B", 10d, 10d, 1d, markerId: 21, rawSampleIndices: new[] { 120 }),
                Sample(5, 4, "Leaf", 10d, 4d, 4d, markerId: 30, rawSampleIndices: new[] { 121 })
            };
            var raw = new[]
            {
                Sample(100, null, "Root", 0d, 20d, 1d, markerId: 10),
                Sample(110, 100, "A", 0d, 10d, 1d, markerId: 20),
                Sample(111, 110, "Leaf", 0d, 4d, 4d, markerId: 30),
                Sample(112, 110, "Leaf", 4d, 4d, 4d, markerId: 30),
                Sample(120, 100, "B", 10d, 10d, 1d, markerId: 21),
                Sample(121, 120, "Leaf", 10d, 4d, 4d, markerId: 30)
            };
            var inverted = new[]
            {
                Sample(200, null, "Inverted Root", 0d, 20d, 0d, markerId: -1),
                Sample(201, 200, "Leaf", 0d, 12d, 12d, markerId: 30,
                    rawSampleIndices: new[] { 111, 112, 121 }),
                Sample(202, 201, "A", 0d, 8d, 0d, markerId: 20, rawSampleIndices: new[] { 110 }),
                Sample(203, 201, "B", 0d, 4d, 0d, markerId: 21, rawSampleIndices: new[] { 120 })
            };
            var thread = new ProfilerThreadRecord(0, 100, "Fixture", "Main Thread", true,
                normal, raw, inverted);
            var frame = Frame(4, 20d, thread);

            var self = ProfilerAnalysis.FrameSelf(frame, null, null, 20, 3, "session-a");
            Assert.That(self.Ok, Is.True);
            Assert.That(self.Result.Rows, Has.Count.EqualTo(1));
            Assert.That(self.Result.Rows[0].MarkerPaths, Is.EqualTo(new[]
            {
                new[] { "Root", "A", "Leaf" },
                new[] { "Root", "B", "Leaf" }
            }));

            var normalAId = ProfilerOpaqueId.Encode(
                new ProfilerOpaqueIdPayload(1, 3, ProfilerViewKind.Normal, 4, 0, 3));
            var sample = ProfilerAnalysis.Sample(frame, thread, normalAId, 3, "session-a");
            Assert.That(sample.Result.Occurrences, Has.Count.EqualTo(2));
            Assert.That(sample.Result.Occurrences,
                Has.All.Property(nameof(SampleSummary.MarkerPath)).EqualTo(new[] { "Root", "A", "Leaf" }));

            var bottom = ProfilerAnalysis.BottomUp(frame, thread, self.Result.Rows[0].BottomUpId, 3, "session-a");
            Assert.That(bottom.Ok, Is.True);
            Assert.That(bottom.Result.MarkerPaths, Is.EqualTo(self.Result.Rows[0].MarkerPaths));
            Assert.That(bottom.Result.Callers.Select(row => row.MarkerPaths.Single()), Is.EqualTo(new[]
            {
                new[] { "Root", "A" },
                new[] { "Root", "B" }
            }));
            Assert.That(bottom.Result.Occurrences.Select(row => row.MarkerPath), Is.EqualTo(new[]
            {
                new[] { "Root", "A", "Leaf" },
                new[] { "Root", "A", "Leaf" },
                new[] { "Root", "B", "Leaf" }
            }));
            Assert.That(StringComparer.Ordinal.Compare(bottom.Result.Occurrences[0].SampleId,
                bottom.Result.Occurrences[1].SampleId), Is.LessThan(0));
        }

        [Test]
        public void BottomUpUsesOpaqueIdsAfterEqualMetricsAndStructuralPaths()
        {
            var raw = new[]
            {
                Sample(100, null, "A", 0d, 4d, 4d),
                Sample(101, null, "B", 0d, 4d, 4d),
                Sample(102, null, "A\u0001B", 0d, 4d, 4d),
                Sample(110, null, "Same", 0d, 4d, 4d),
                Sample(111, null, "Same", 0d, 4d, 4d)
            };
            var earlyCallerItem = EarlierOpaqueItem(202, 203, ProfilerViewKind.BottomUp);
            var lateCallerItem = earlyCallerItem == 202 ? 203 : 202;
            var inverted = new[]
            {
                Sample(200, null, "Inverted Root", 0d, 20d, 0d),
                Sample(201, 200, "Same", 0d, 8d, 8d, rawSampleIndices: new[] { 110, 111 }),
                Sample(earlyCallerItem, 201, "Later", 0d, 4d, 0d, rawSampleIndices: new[] { 102 }),
                Sample(lateCallerItem, 201, "Earlier", 0d, 4d, 0d, rawSampleIndices: new[] { 100, 101 })
            };
            var thread = new ProfilerThreadRecord(0, 100, "Fixture", "Main Thread", true,
                Array.Empty<ProfilerSampleRecord>(), raw, inverted);
            var frame = Frame(4, 20d, thread);
            var bottomUpId = ProfilerOpaqueId.Encode(
                new ProfilerOpaqueIdPayload(1, 3, ProfilerViewKind.BottomUp, 4, 0, 201));

            var result = ProfilerAnalysis.BottomUp(frame, thread, bottomUpId, 3, "session-a");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.Callers[0].MarkerPaths,
                Is.EqualTo(new[] { new[] { "A" }, new[] { "B" } }));
            Assert.That(result.Result.Callers[1].MarkerPaths,
                Is.EqualTo(new[] { new[] { "A\u0001B" } }));
            Assert.That(StringComparer.Ordinal.Compare(result.Result.Occurrences[0].SampleId,
                result.Result.Occurrences[1].SampleId), Is.LessThan(0));
        }

        [Test]
        public void BottomUpCallerTieUsesOpaqueIdAfterIdenticalCanonicalPathSets()
        {
            var raw = new[]
            {
                Sample(100, null, "Root", 0d, 4d, 0d),
                Sample(101, 100, "Caller", 0d, 4d, 4d),
                Sample(110, null, "Leaf", 0d, 8d, 8d)
            };
            var earlyCallerItem = EarlierOpaqueItem(202, 203, ProfilerViewKind.BottomUp);
            var lateCallerItem = earlyCallerItem == 202 ? 203 : 202;
            var inverted = new[]
            {
                Sample(200, null, "Inverted Root", 0d, 20d, 0d),
                Sample(201, 200, "Leaf", 0d, 8d, 8d, rawSampleIndices: new[] { 110 }),
                // Insert the later opaque ID first. Equal metrics and identical path sets must
                // leave the final ordering decision to the encoded ID rather than source order.
                Sample(lateCallerItem, 201, "Caller", 0d, 4d, 0d, rawSampleIndices: new[] { 101 }),
                Sample(earlyCallerItem, 201, "Caller", 0d, 4d, 0d, rawSampleIndices: new[] { 101 })
            };
            var thread = new ProfilerThreadRecord(0, 100, "Fixture", "Main Thread", true,
                Array.Empty<ProfilerSampleRecord>(), raw, inverted);
            var frame = Frame(4, 20d, thread);
            var bottomUpId = ProfilerOpaqueId.Encode(
                new ProfilerOpaqueIdPayload(1, 3, ProfilerViewKind.BottomUp, 4, 0, 201));

            var result = ProfilerAnalysis.BottomUp(frame, thread, bottomUpId, 3, "session-a");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.Callers, Has.Count.EqualTo(2));
            Assert.That(result.Result.Callers[0].MarkerPaths,
                Is.EqualTo(result.Result.Callers[1].MarkerPaths));
            Assert.That(StringComparer.Ordinal.Compare(result.Result.Callers[0].BottomUpId,
                result.Result.Callers[1].BottomUpId), Is.LessThan(0));
            Assert.That(ProfilerOpaqueId.Decode(result.Result.Callers[0].BottomUpId, 3,
                ProfilerViewKind.BottomUp).Result.ItemId, Is.EqualTo(earlyCallerItem));
        }

        [Test]
        public void TimeRowsUseMetricThenPathThenOpaqueIdOrderingAndSafeZeroPercent()
        {
            var frame = Frame(4, 0d, Thread("Main Thread",
                Sample(9, null, "Root", 0d, 0d, 0d),
                Sample(2, 9, "Beta", 0d, 8d, 3d),
                Sample(3, 9, "Alpha", 0d, 8d, 4d)));

            var result = ProfilerAnalysis.FrameTotal(frame, 16d, null, null, 20, 3, "session-a");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.Rows[0].MarkerPath, Is.EqualTo(new[] { "Root", "Alpha" }));
            Assert.That(result.Result.Rows[1].MarkerPath, Is.EqualTo(new[] { "Root", "Beta" }));
            Assert.That(result.Result.Rows[0].PercentOfFrame, Is.Null);
        }

        [Test]
        public void FrameTotalEchoesEqualityBudgetAndLimitsOnlyReturnedRows()
        {
            var frame = Frame(4, 16d, Thread("Main Thread",
                Sample(1, null, "Root", 0d, 16d, 1d),
                Sample(2, 1, "Leaf", 0d, 8d, 8d)));

            var result = ProfilerAnalysis.FrameTotal(frame, 16d, null, null, 1, 3, "session-a");

            Assert.That(result.Result.OverBudget, Is.False);
            Assert.That(result.Result.OverBudgetMs, Is.Zero);
            Assert.That(result.Result.BudgetRatio, Is.EqualTo(1d));
            Assert.That(result.Result.Rows.Count, Is.EqualTo(1));
            Assert.That(result.Result.AvailableRowCount, Is.EqualTo(2));
            Assert.That(result.Result.Truncated, Is.True);
            Assert.That(result.Result.Rows[0].TotalMs, Is.LessThanOrEqualTo(result.Result.FrameTimeMs));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(0d)]
        [TestCase(-1d)]
        public void FrameTotalRejectsEveryNonFiniteOrNonPositiveTarget(double targetMs)
        {
            var frame = Frame(1, 10d, Thread("Main Thread", Sample(1, null, "Root", 0d, 10d, 1d)));

            Assert.That(ProfilerAnalysis.FrameTotal(frame, targetMs, null, null, 20, 1, "session-a").Error.Code,
                Is.EqualTo("TARGET_MS_INVALID"));
        }

        [TestCase(1e-320d)]
        [TestCase(1e-306d)]
        public void FrameTotalRefusesUnrepresentableBudgetArithmeticWithoutPartialResult(double targetMs)
        {
            var frame = Frame(1, 10d, Thread("Main Thread", Sample(1, null, "Root", 0d, 10d, 1d)));

            var result = ProfilerAnalysis.FrameTotal(frame, targetMs, null, null, 20, 1, "session-a");

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Result, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("PROFILER_DATA_UNAVAILABLE"));
        }

        [Test]
        public void FrameTotalAcceptsSmallPositiveTargetWithRepresentableBudgetArithmetic()
        {
            var frame = Frame(1, 10d, Thread("Main Thread", Sample(1, null, "Root", 0d, 10d, 1d)));

            var result = ProfilerAnalysis.FrameTotal(frame, 0.001d, null, null, 20, 1, "session-a");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.BudgetRatio, Is.EqualTo(10000d));
            Assert.That(result.Result.Rows[0].PercentOfBudget, Is.EqualTo(1000000d));
        }

        [Test]
        public void ThreadSelectionRequiresOneExactUnambiguousSelector()
        {
            var frame = Frame(1, 10d,
                Thread("Main Thread", Sample(1, null, "Root", 0d, 10d, 1d)),
                Thread("Worker", new[] { Sample(2, null, "Root", 0d, 10d, 1d) }, threadIndex: 1),
                Thread("Worker", new[] { Sample(3, null, "Root", 0d, 10d, 1d) }, threadIndex: 2));

            Assert.That(ProfilerAnalysis.SelectThread(frame, 1, "Worker").Error.Code,
                Is.EqualTo("THREAD_SELECTOR_CONFLICT"));
            Assert.That(ProfilerAnalysis.SelectThread(frame, null, "Worker").Error.Code,
                Is.EqualTo("THREAD_AMBIGUOUS"));
            Assert.That(ProfilerAnalysis.SelectThread(frame, 1, null).Result.ThreadIndex, Is.EqualTo(1));
            Assert.That(ProfilerAnalysis.SelectThread(frame, null, "Missing").Error.Code,
                Is.EqualTo("THREAD_NOT_FOUND"));

            var duplicateDefault = Frame(2, 10d,
                Thread("Main Thread", new[] { Sample(4, null, "Root", 0d, 10d, 1d) }, threadIndex: 0),
                Thread("Main Thread", new[] { Sample(5, null, "Root", 0d, 10d, 1d) }, threadIndex: 1));
            Assert.That(ProfilerAnalysis.SelectThread(duplicateDefault, null, null).Error.Code,
                Is.EqualTo("THREAD_AMBIGUOUS"));
        }

        [Test]
        public void MarkerPathLookupIsExactAndRejectsAmbiguity()
        {
            var frame = Frame(2, 20d, Thread("Main Thread",
                Sample(1, null, "Root", 0d, 20d, 1d),
                Sample(2, 1, "Branch", 0d, 10d, 1d),
                Sample(3, 2, "Leaf", 0d, 5d, 2d),
                Sample(4, 1, "Branch", 10d, 10d, 1d),
                Sample(5, 4, "Leaf", 10d, 5d, 2d)));

            Assert.That(ProfilerAnalysis.FindMarkerPath(frame.Threads[0], new[] { "Root", "Missing" }).Error.Code,
                Is.EqualTo("MARKER_PATH_NOT_FOUND"));
            Assert.That(ProfilerAnalysis.FindMarkerPath(frame.Threads[0], new[] { "Root", "Branch", "Leaf" }).Error.Code,
                Is.EqualTo("MARKER_PATH_AMBIGUOUS"));
        }

        [Test]
        public void OpaqueIdsRoundTripAndRejectCorruptionKindAndEpoch()
        {
            var payload = new ProfilerOpaqueIdPayload(1, 17, ProfilerViewKind.Normal, 9, 2, 41);
            var encoded = ProfilerOpaqueId.Encode(payload);

            Assert.That(ProfilerOpaqueId.Decode(encoded, 17, ProfilerViewKind.Normal).Result.ItemId, Is.EqualTo(41));
            Assert.That(ProfilerOpaqueId.Decode(encoded + "x", 17, ProfilerViewKind.Normal).Error.Code,
                Is.EqualTo("SAMPLE_ID_INVALID"));
            Assert.That(ProfilerOpaqueId.Decode(encoded, 18, ProfilerViewKind.Normal).Error.Code,
                Is.EqualTo("SAMPLE_ID_STALE"));
            Assert.That(ProfilerOpaqueId.Decode(encoded, 17, ProfilerViewKind.BottomUp).Error.Code,
                Is.EqualTo("SAMPLE_ID_KIND_MISMATCH"));
        }

        [Test]
        public void FrameGcCountsNestedRawEventsOnceAndThresholdsDirectNormalSites()
        {
            var available = AllocationFrame(3, 1000, 9000);
            var result = ProfilerAnalysis.FrameGc(available, null, null, 8192, 4, "session-a");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.SessionId, Is.EqualTo("session-a"));
            Assert.That(result.Result.FrameIndex, Is.EqualTo(3));
            Assert.That(result.Result.Thread.ThreadName, Is.EqualTo("Main Thread"));
            Assert.That(result.Result.TotalBytes, Is.EqualTo(10000));
            Assert.That(result.Result.TotalCount, Is.EqualTo(2));
            Assert.That(result.Result.ReportedBytes, Is.EqualTo(9000));
            Assert.That(result.Result.ReportedCount, Is.EqualTo(1));
            Assert.That(result.Result.Rows.Count, Is.EqualTo(1));
            Assert.That(result.Result.Rows[0].MarkerPath, Is.EqualTo(new[] { "Root", "Parent", "Child" }));
            Assert.That(ProfilerOpaqueId.Decode(result.Result.Rows[0].SampleId, 4, ProfilerViewKind.Normal).Ok,
                Is.True);

            var inclusive = ProfilerAnalysis.SampleGc(available, available.Threads[0],
                NormalId(3, 4, 2), 4, "session-a");
            Assert.That(inclusive.Result.TotalBytes, Is.EqualTo(10000));
            Assert.That(inclusive.Result.TotalCount, Is.EqualTo(2));
            Assert.That(inclusive.Result.DirectBytes, Is.EqualTo(1000));
            Assert.That(inclusive.Result.DirectCount, Is.EqualTo(1));

            var child = ProfilerAnalysis.MarkerGc(available, available.Threads[0],
                new[] { "Root", "Parent", "Child" }, 4, "session-a");
            Assert.That(child.Result.TotalBytes, Is.EqualTo(9000));
            Assert.That(child.Result.DirectBytes, Is.EqualTo(9000));
            Assert.That(child.Schema, Is.EqualTo(ProfilerSchema.GcMarker));
        }

        [Test]
        public void GcDistinguishesRecordedZeroFromUnavailableMetadataAndRejectsNegativeThreshold()
        {
            var zero = Frame(3, 10d, Thread("Main Thread", Array.Empty<ProfilerSampleRecord>(),
                allocationDataAvailable: true));
            var zeroResult = ProfilerAnalysis.FrameGc(zero, null, null, 8192, 4, "session-a");
            Assert.That(zeroResult.Ok, Is.True);
            Assert.That(zeroResult.Result.TotalBytes, Is.Zero);
            Assert.That(zeroResult.Result.TotalCount, Is.Zero);
            Assert.That(zeroResult.Result.Rows, Is.Empty);

            var unavailable = Frame(3, 10d, Thread("Main Thread", false,
                Sample(1, null, "Root", 0d, 10d, 1d)));
            var missing = ProfilerAnalysis.FrameGc(unavailable, null, null, 0, 4, "session-a");
            Assert.That(missing.Ok, Is.False);
            Assert.That(missing.Result, Is.Null);
            Assert.That(missing.Error.Code, Is.EqualTo("ALLOCATION_DATA_UNAVAILABLE"));

            var negative = ProfilerAnalysis.FrameGc(zero, null, null, -1, 4, "session-a");
            Assert.That(negative.Ok, Is.False);
            Assert.That(negative.Result, Is.Null);
            Assert.That(negative.Error.Code, Is.EqualTo("THRESHOLD_BYTES_INVALID"));
        }

        [Test]
        public void FrameGcUsesIndexedNormalAllocationNodeWhenItsEnclosingSiteCannotBeProven()
        {
            var normal = new[]
            {
                Sample(1, null, "GC.Alloc", 1d, 1d, 1d, markerId: 99, rawSampleIndices: new[] { 100 })
            };
            var raw = new[] { Sample(100, null, "GC.Alloc", 1d, 1d, 1d, 4096, 1, markerId: 99) };
            var frame = Frame(3, 10d, new ProfilerThreadRecord(0, 100, "Fixture", "Main Thread", true,
                normal, raw, Array.Empty<ProfilerSampleRecord>()));

            var result = ProfilerAnalysis.FrameGc(frame, null, null, 0, 4, "session-a");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.TotalBytes, Is.EqualTo(4096));
            Assert.That(result.Result.Rows.Single().MarkerPath, Is.EqualTo(new[] { "GC.Alloc" }));
        }

        [Test]
        public void FrameGcWalksPastUnrepresentedRawParentToNearestIndexedNormalAncestor()
        {
            var normal = new[]
            {
                Sample(1, null, "Root", 0d, 10d, 1d, markerId: 10, rawSampleIndices: new[] { 100 }),
                Sample(2, 1, "Owner", 0d, 9d, 1d, markerId: 20, rawSampleIndices: new[] { 110 })
            };
            var raw = new[]
            {
                Sample(100, null, "Root", 0d, 10d, 1d, markerId: 10),
                Sample(110, 100, "Owner", 0d, 9d, 1d, markerId: 20),
                Sample(120, 110, "Unrepresented", 0d, 8d, 1d, markerId: 30),
                Sample(121, 120, "GC.Alloc", 0d, 0d, 0d, 4096, 1, markerId: 99)
            };
            var frame = Frame(3, 10d, new ProfilerThreadRecord(0, 100, "Fixture", "Main Thread", true,
                normal, raw, Array.Empty<ProfilerSampleRecord>()));

            var result = ProfilerAnalysis.FrameGc(frame, null, null, 0, 4, "session-a");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.Rows.Single().MarkerPath, Is.EqualTo(new[] { "Root", "Owner" }));
        }

        [Test]
        public void FrameGcRefusesMarkerAndPathMatchWithoutIndexedOwnership()
        {
            var normal = new[] { Sample(1, null, "Site", 0d, 10d, 1d, markerId: 20) };
            var raw = new[]
            {
                Sample(100, null, "Site", 0d, 10d, 1d, markerId: 20),
                Sample(101, 100, "GC.Alloc", 0d, 0d, 0d, 4096, 1, markerId: 99)
            };
            var frame = Frame(3, 10d, new ProfilerThreadRecord(0, 100, "Fixture", "Main Thread", true,
                normal, raw, Array.Empty<ProfilerSampleRecord>()));

            var result = ProfilerAnalysis.FrameGc(frame, null, null, 0, 4, "session-a");

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Result, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("PROFILER_DATA_UNAVAILABLE"));
        }

        [Test]
        public void SampleAndGcSampleKeepEqualPathsSeparatedByIndexedRawSubtrees()
        {
            var normal = new[]
            {
                Sample(1, null, "Root", 0d, 20d, 1d, rawSampleIndices: new[] { 100 }),
                Sample(2, 1, "Site", 0d, 8d, 2d, markerId: 20, rawSampleIndices: new[] { 110 }),
                Sample(3, 1, "Site", 10d, 8d, 2d, markerId: 20, rawSampleIndices: new[] { 120 })
            };
            var raw = new[]
            {
                Sample(100, null, "Root", 0d, 20d, 1d),
                Sample(110, 100, "Site", 0d, 8d, 2d, markerId: 20),
                Sample(111, 110, "GC.Alloc", 0d, 0d, 0d, 1000, 1, markerId: 99),
                Sample(120, 100, "Site", 10d, 8d, 2d, markerId: 20),
                Sample(121, 120, "GC.Alloc", 10d, 0d, 0d, 2000, 1, markerId: 99)
            };
            var thread = new ProfilerThreadRecord(0, 100, "Fixture", "Main Thread", true,
                normal, raw, Array.Empty<ProfilerSampleRecord>());
            var frame = Frame(3, 20d, thread);

            var firstTime = ProfilerAnalysis.Sample(frame, thread, NormalId(3, 4, 2), 4, "session-a");
            var secondTime = ProfilerAnalysis.Sample(frame, thread, NormalId(3, 4, 3), 4, "session-a");
            var firstGc = ProfilerAnalysis.SampleGc(frame, thread, NormalId(3, 4, 2), 4, "session-a");
            var secondGc = ProfilerAnalysis.SampleGc(frame, thread, NormalId(3, 4, 3), 4, "session-a");

            Assert.That(firstTime.Result.Occurrences.Select(row => row.SampleId),
                Is.EqualTo(new[] { RawId(3, 4, 110) }));
            Assert.That(secondTime.Result.Occurrences.Select(row => row.SampleId),
                Is.EqualTo(new[] { RawId(3, 4, 120) }));
            Assert.That(firstGc.Result.TotalBytes, Is.EqualTo(1000));
            Assert.That(firstGc.Result.DirectBytes, Is.EqualTo(1000));
            Assert.That(secondGc.Result.TotalBytes, Is.EqualTo(2000));
            Assert.That(secondGc.Result.DirectBytes, Is.EqualTo(2000));
        }

        [Test]
        public void TimeAndGcRowsOrderDelimiterBearingPathsStructurally()
        {
            var earlyItem = EarlierOpaqueItem(2, 3, ProfilerViewKind.Normal);
            var lateItem = earlyItem == 2 ? 3 : 2;
            var normal = new[]
            {
                Sample(1, null, "Root", 0d, 10d, 0d, rawSampleIndices: new[] { 100 }),
                Sample(earlyItem, 1, "A\0B", 0d, 4d, 4d, markerId: 20, rawSampleIndices: new[] { 120 }),
                Sample(lateItem, 1, "A", 4d, 4d, 4d, markerId: 21, rawSampleIndices: new[] { 110 }),
                Sample(4, lateItem, "B", 4d, 4d, 4d, markerId: 22, rawSampleIndices: new[] { 111 })
            };
            var raw = new[]
            {
                Sample(100, null, "Root", 0d, 10d, 0d),
                Sample(110, 100, "A", 4d, 4d, 0d, markerId: 21),
                Sample(111, 110, "B", 4d, 4d, 4d, markerId: 22),
                Sample(112, 111, "GC.Alloc", 4d, 0d, 0d, 1000, 1, markerId: 99),
                Sample(120, 100, "A\0B", 0d, 4d, 4d, markerId: 20),
                Sample(121, 120, "GC.Alloc", 0d, 0d, 0d, 1000, 1, markerId: 99)
            };
            var thread = new ProfilerThreadRecord(0, 100, "Fixture", "Main Thread", true,
                normal, raw, Array.Empty<ProfilerSampleRecord>());
            var frame = Frame(4, 10d, thread);

            var timing = ProfilerAnalysis.FrameTotal(frame, 16d, null, null, 20, 3, "session-a");
            var gc = ProfilerAnalysis.FrameGc(frame, null, null, 0, 3, "session-a");

            Assert.That(timing.Result.Rows.Where(row => row.MarkerPath.Last() == "B" ||
                    row.MarkerPath.Last() == "A\0B").Select(row => row.MarkerPath),
                Is.EqualTo(new[] { new[] { "Root", "A", "B" }, new[] { "Root", "A\0B" } }));
            Assert.That(gc.Result.Rows.Select(row => row.MarkerPath),
                Is.EqualTo(new[] { new[] { "Root", "A", "B" }, new[] { "Root", "A\0B" } }));
        }

        [Test]
        public void OverallGcAppliesThresholdPerFrameThreadSiteBeforeCombiningPaths()
        {
            var frames = new[]
            {
                AllocationFrame(1, 5000, 8192),
                AllocationFrame(2, 5000, 8192)
            };

            var result = ProfilerAnalysis.OverallGc(frames, 8192, 4, "session-a");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.FrameCount, Is.EqualTo(2));
            Assert.That(result.Result.TotalBytes, Is.EqualTo(26384));
            Assert.That(result.Result.TotalCount, Is.EqualTo(4));
            Assert.That(result.Result.ReportedBytes, Is.EqualTo(16384));
            Assert.That(result.Result.ReportedCount, Is.EqualTo(2));
            Assert.That(result.Result.Rows, Has.Count.EqualTo(1));
            Assert.That(result.Result.Rows[0].Bytes, Is.EqualTo(16384));
            Assert.That(result.Result.Rows[0].Count, Is.EqualTo(2));
            Assert.That(result.Result.Rows[0].MarkerPath,
                Is.EqualTo(new[] { "Root", "Parent", "Child" }));
        }

        [Test]
        public void RangeGcIncludesZeroFramesUsesExactMedianAndLowestMaximumTie()
        {
            var frames = new[]
            {
                AllocationFrame(7, 1000, 9000),
                AllocationFrame(8, 0, 0),
                AllocationFrame(9, 10000, 10000),
                AllocationFrame(10, 1000, 9000)
            };

            var result = ProfilerAnalysis.RangeGc(frames, 7, 10, 8192, "session-a");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.FirstFrameIndex, Is.EqualTo(7));
            Assert.That(result.Result.LastFrameIndex, Is.EqualTo(10));
            Assert.That(result.Result.FrameCount, Is.EqualTo(4));
            Assert.That(result.Result.TotalBytes, Is.EqualTo(40000));
            Assert.That(result.Result.TotalCount, Is.EqualTo(6));
            Assert.That(result.Result.ReportedBytes, Is.EqualTo(38000));
            Assert.That(result.Result.ReportedCount, Is.EqualTo(4));
            Assert.That(result.Result.MinBytes, Is.Zero);
            Assert.That(result.Result.MedianBytes, Is.EqualTo(10000m));
            Assert.That(result.Result.P95Bytes, Is.EqualTo(20000));
            Assert.That(result.Result.MaxBytes, Is.EqualTo(20000));
            Assert.That(result.Result.MaxFrameIndex, Is.EqualTo(9));
            Assert.That(result.Result.PercentileMethod, Is.EqualTo("nearest-rank"));
            Assert.That(result.Result.Frames.Select(value => value.TotalBytes),
                Is.EqualTo(new long[] { 10000, 0, 20000, 10000 }));
        }

        [Test]
        public void RangeGcPreservesHalfByteMedianWithoutOverflow()
        {
            var frames = new[]
            {
                AllocationFrame(1, 4, 0),
                AllocationFrame(2, 5, 0)
            };

            var result = ProfilerAnalysis.RangeGc(frames, 1, 2, 0, "session-a");

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.MedianBytes, Is.EqualTo(4.5m));
        }

        [TestCase(long.MaxValue, 1L)]
        [TestCase(1L, long.MaxValue)]
        public void GcArithmeticOverflowReturnsNoPartialResult(long first, long second)
        {
            var frame = AllocationFrame(3, first, second);

            var result = ProfilerAnalysis.FrameGc(frame, null, null, 0, 4, "session-a");

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Result, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("PROFILER_DATA_UNAVAILABLE"));
        }

        [Test]
        public void GcSampleRejectsRawBottomUpWrongFrameAndStaleIdsWithoutPartialResults()
        {
            var frame = AllocationFrame(3, 1000, 9000);
            var ids = new[]
            {
                ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, 4, ProfilerViewKind.Raw, 3, 0, 110)),
                ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, 4, ProfilerViewKind.BottomUp, 3, 0, 110)),
                ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, 4, ProfilerViewKind.Normal, 4, 0, 2)),
                ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, 3, ProfilerViewKind.Normal, 3, 0, 2))
            };
            var expected = new[]
            {
                "SAMPLE_ID_KIND_MISMATCH", "SAMPLE_ID_KIND_MISMATCH", "SAMPLE_ID_INVALID", "SAMPLE_ID_STALE"
            };

            for (var index = 0; index < ids.Length; index++)
            {
                var result = ProfilerAnalysis.SampleGc(frame, frame.Threads[0], ids[index], 4, "session-a");
                Assert.That(result.Ok, Is.False);
                Assert.That(result.Result, Is.Null);
                Assert.That(result.Error.Code, Is.EqualTo(expected[index]));
            }
        }

        [Test]
        public void GcMarkerPathRejectsMissingAndDuplicateExactPathsWithoutPartialResults()
        {
            var normal = new[]
            {
                Sample(1, null, "Root", 0d, 10d, 1d, markerId: 10),
                Sample(2, 1, "Site", 0d, 4d, 1d, markerId: 20),
                Sample(3, 1, "Site", 4d, 4d, 1d, markerId: 20)
            };
            var frame = Frame(3, 10d, new ProfilerThreadRecord(0, 100, "Fixture", "Main Thread", true,
                normal, Array.Empty<ProfilerSampleRecord>(), Array.Empty<ProfilerSampleRecord>()));

            var missing = ProfilerAnalysis.MarkerGc(frame, frame.Threads[0],
                new[] { "Root", "Missing" }, 4, "session-a");
            var ambiguous = ProfilerAnalysis.MarkerGc(frame, frame.Threads[0],
                new[] { "Root", "Site" }, 4, "session-a");

            Assert.That(missing.Result, Is.Null);
            Assert.That(missing.Error.Code, Is.EqualTo("MARKER_PATH_NOT_FOUND"));
            Assert.That(ambiguous.Result, Is.Null);
            Assert.That(ambiguous.Error.Code, Is.EqualTo("MARKER_PATH_AMBIGUOUS"));
        }

        [Test]
        public void RelatedSamplesUseHalfOpenPositiveOverlapAndNeverInventFlow()
        {
            var source = Sample(10, null, "Source", 10d, 10d, 1d);
            var target = Thread("Worker", new[]
            {
                Sample(20, null, "EndsAtStart", 0d, 10d, 1d),
                Sample(21, null, "Overlap", 15d, 10d, 1d),
                Sample(22, null, "StartsAtEnd", 20d, 4d, 1d)
            }, threadIndex: 1);

            var sourceThread = Thread("Main Thread", new[] { source }, threadIndex: 0);
            var frame = Frame(5, 30d, sourceThread, target);
            var sourceId = ProfilerOpaqueId.Encode(
                new ProfilerOpaqueIdPayload(1, 7, ProfilerViewKind.Raw, 5, 0, 10));
            var rows = ProfilerAnalysis.Related(frame, sourceThread, sourceId, target, 7, "session-a");

            Assert.That(rows.Result.Rows.Count, Is.EqualTo(1));
            Assert.That(rows.Result.Rows[0].MarkerPath, Is.EqualTo(new[] { "Overlap" }));
            Assert.That(rows.Result.Rows[0].OverlapMs, Is.EqualTo(5d));
            Assert.That(rows.Result.Rows[0].Relation, Is.EqualTo("temporal-overlap"));
        }

        [Test]
        public void RelatedRefusesTimestampArithmeticOverflowWithoutPartialResult()
        {
            var source = new ProfilerSampleRecord(10, null, "Source", long.MaxValue - 5, 10, 10, 1,
                0, 0, false, Array.Empty<long>());
            var sourceThread = Thread("Main Thread", new[] { source }, threadIndex: 0);
            var target = Thread("Worker", new[] { Sample(20, null, "Target", 0d, 1d, 1d) }, threadIndex: 1);
            var frame = Frame(5, 30d, sourceThread, target);
            var sourceId = ProfilerOpaqueId.Encode(
                new ProfilerOpaqueIdPayload(1, 7, ProfilerViewKind.Raw, 5, 0, 10));

            var result = ProfilerAnalysis.Related(frame, sourceThread, sourceId, target, 7, "session-a");

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Result, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("PROFILER_DATA_UNAVAILABLE"));
        }

        [Test]
        public void InvestigationCompositionPreservesLinkedIdsAndAcceptsAbsoluteOrRelativeTolerance()
        {
            var thread = new ThreadIdentity(0, 100, "Fixture", "Main Thread");
            var leafRow = new SelfTimeRow("bottom-id", new[] { new[] { "Root", "Leaf" } }, 10d, 10d, 1);
            var leaf = BatihanDev.UnityCliCommands.Foundation.CommandResult<FrameSelfResult>.Success(
                "unity.profiler.frame-self@1", new FrameSelfResult("session-a", 4, thread,
                    new[] { leafRow }, 1));
            var bottom = BatihanDev.UnityCliCommands.Foundation.CommandResult<BottomUpResult>.Success(
                "unity.profiler.bottom-up@1", new BottomUpResult("session-a", 4, thread, "bottom-id", "Leaf",
                    new[] { new[] { "Root", "Leaf" } }, 10d, 10.009d, 1, 100d,
                    Array.Empty<SelfTimeRow>(), Array.Empty<SampleSummary>()));
            var marker = BatihanDev.UnityCliCommands.Foundation.CommandResult<MarkerTimeResult>.Success(
                "unity.profiler.marker-time@1", new MarkerTimeResult("session-a", 4, thread, "normal-id",
                    new[] { "Root", "Leaf" }, 100.9d, 10d, 1));

            var result = ProfilerAnalysis.ComposeInvestigation(leaf, bottom, marker,
                new[] { "Root", "Leaf" }, 0.01d, 1d);

            Assert.That(result.Ok, Is.True);
            Assert.That(result.Result.LinkedIds.BottomUpId, Is.EqualTo("bottom-id"));
            Assert.That(result.Result.LinkedIds.MarkerSampleId, Is.EqualTo("normal-id"));
            Assert.That(result.Result.Reconciliation.LeafMatchesBottomUp, Is.True);
            Assert.That(result.Result.Reconciliation.CallersWithinMarker, Is.True);
            Assert.That(result.Result.Leaf.Schema, Is.EqualTo(ProfilerSchema.FrameSelf));
            Assert.That(result.Result.BottomUp.Schema, Is.EqualTo(ProfilerSchema.BottomUp));
            Assert.That(result.Result.Marker.Schema, Is.EqualTo(ProfilerSchema.MarkerTime));
        }

        [Test]
        public void InvestigationCompositionRefusesMultiPathAggregateWithCandidates()
        {
            var thread = new ThreadIdentity(0, 100, "Fixture", "Main Thread");
            var row = new SelfTimeRow("bottom-id", new[]
            {
                new[] { "Root", "A", "Leaf" },
                new[] { "Root", "B", "Leaf" }
            }, 10d, 10d, 1);
            var leaf = BatihanDev.UnityCliCommands.Foundation.CommandResult<FrameSelfResult>.Success(
                ProfilerSchema.FrameSelf, new FrameSelfResult("session-a", 4, thread, new[] { row }, 1));

            var result = ProfilerAnalysis.ComposeInvestigation(leaf, null, null,
                new[] { "Root", "A", "Leaf" }, 0.01d, 1d);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Result, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("MARKER_PATH_AMBIGUOUS"));
            Assert.That(result.Error.Details["step"], Is.EqualTo("leaf"));
            Assert.That(result.Error.Details["command"], Is.EqualTo("profiler.time.frame-self"));
            Assert.That(result.Error.Details["candidates"], Is.EqualTo(row.MarkerPaths));
        }

        [Test]
        public void InvestigationCompositionReturnsNoPartialResultForSourceOrReconciliationFailure()
        {
            var sourceFailure = BatihanDev.UnityCliCommands.Foundation.CommandResult<FrameSelfResult>.Failure(
                "unity.profiler.frame-self@1", "THREAD_NOT_FOUND", "missing");
            var failedSource = ProfilerAnalysis.ComposeInvestigation(sourceFailure, null, null,
                new[] { "Root", "Leaf" }, 0.01d, 1d);
            Assert.That(failedSource.Result, Is.Null);
            Assert.That(failedSource.Error.Details["step"], Is.EqualTo("leaf"));
            Assert.That(failedSource.Error.Details["command"], Is.EqualTo("profiler.time.frame-self"));
            Assert.That(failedSource.Error.Details["sourceCode"], Is.EqualTo("THREAD_NOT_FOUND"));

            var thread = new ThreadIdentity(0, 100, "Fixture", "Main Thread");
            var row = new SelfTimeRow("bottom-id", new[] { new[] { "Root", "Leaf" } }, 10d, 10d, 1);
            var leaf = BatihanDev.UnityCliCommands.Foundation.CommandResult<FrameSelfResult>.Success(
                "unity.profiler.frame-self@1", new FrameSelfResult("session-a", 4, thread, new[] { row }, 1));
            var bottom = BatihanDev.UnityCliCommands.Foundation.CommandResult<BottomUpResult>.Success(
                "unity.profiler.bottom-up@1", new BottomUpResult("session-a", 4, thread, "bottom-id", "Leaf",
                    new[] { new[] { "Root", "Leaf" } }, 10d, 12d, 1, 80d,
                    Array.Empty<SelfTimeRow>(), Array.Empty<SampleSummary>()));
            var marker = BatihanDev.UnityCliCommands.Foundation.CommandResult<MarkerTimeResult>.Success(
                "unity.profiler.marker-time@1", new MarkerTimeResult("session-a", 4, thread, "normal-id",
                    new[] { "Root", "Leaf" }, 100d, 10d, 1));

            var mismatch = ProfilerAnalysis.ComposeInvestigation(leaf, bottom, marker,
                new[] { "Root", "Leaf" }, 0.01d, 1d);
            Assert.That(mismatch.Ok, Is.False);
            Assert.That(mismatch.Result, Is.Null);
            Assert.That(mismatch.Error.Code, Is.EqualTo("PROFILER_DATA_UNAVAILABLE"));
            Assert.That(mismatch.Error.Details["step"], Is.EqualTo("reconciliation"));
        }

        [Test]
        public void InvestigationCompositionReturnsNoPartialResultForBottomUpOrMarkerFailure()
        {
            var thread = new ThreadIdentity(0, 100, "Fixture", "Main Thread");
            var row = new SelfTimeRow("bottom-id", new[] { new[] { "Root", "Leaf" } }, 10d, 10d, 1);
            var leaf = BatihanDev.UnityCliCommands.Foundation.CommandResult<FrameSelfResult>.Success(
                ProfilerSchema.FrameSelf, new FrameSelfResult("session-a", 4, thread, new[] { row }, 1));
            var bottomFailure = BatihanDev.UnityCliCommands.Foundation.CommandResult<BottomUpResult>.Failure(
                ProfilerSchema.BottomUp, "SAMPLE_ID_STALE", "stale");
            var markerFailure = BatihanDev.UnityCliCommands.Foundation.CommandResult<MarkerTimeResult>.Failure(
                ProfilerSchema.MarkerTime, "MARKER_PATH_NOT_FOUND", "missing");

            var bottomResult = ProfilerAnalysis.ComposeInvestigation(leaf, bottomFailure, null,
                new[] { "Root", "Leaf" }, 0.01d, 1d);
            Assert.That(bottomResult.Ok, Is.False);
            Assert.That(bottomResult.Result, Is.Null);
            Assert.That(bottomResult.Error.Details["step"], Is.EqualTo("bottomUp"));
            Assert.That(bottomResult.Error.Details["command"], Is.EqualTo("profiler.time.bottom-up"));
            Assert.That(bottomResult.Error.Details["sourceCode"], Is.EqualTo("SAMPLE_ID_STALE"));

            var bottom = BatihanDev.UnityCliCommands.Foundation.CommandResult<BottomUpResult>.Success(
                ProfilerSchema.BottomUp, new BottomUpResult("session-a", 4, thread, "bottom-id", "Leaf",
                    new[] { new[] { "Root", "Leaf" } }, 10d, 10d, 1, 10d,
                    Array.Empty<SelfTimeRow>(), Array.Empty<SampleSummary>()));
            var markerResult = ProfilerAnalysis.ComposeInvestigation(leaf, bottom, markerFailure,
                new[] { "Root", "Leaf" }, 0.01d, 1d);
            Assert.That(markerResult.Ok, Is.False);
            Assert.That(markerResult.Result, Is.Null);
            Assert.That(markerResult.Error.Details["step"], Is.EqualTo("marker"));
            Assert.That(markerResult.Error.Details["command"], Is.EqualTo("profiler.time.marker-path"));
            Assert.That(markerResult.Error.Details["sourceCode"], Is.EqualTo("MARKER_PATH_NOT_FOUND"));
        }

        [TestCase("wrong.leaf@1", "unity.profiler.bottom-up@1", "unity.profiler.marker-time@1")]
        [TestCase("unity.profiler.frame-self@1", "wrong.bottom@1", "unity.profiler.marker-time@1")]
        [TestCase("unity.profiler.frame-self@1", "unity.profiler.bottom-up@1", "wrong.marker@1")]
        public void InvestigationCompositionRejectsWrongSourceSchemas(
            string leafSchema, string bottomSchema, string markerSchema)
        {
            var sources = InvestigationSources(leafSchema, bottomSchema, markerSchema,
                "bottom-id", "bottom-id", "normal-id");

            var result = ProfilerAnalysis.ComposeInvestigation(sources.Leaf, sources.Bottom, sources.Marker,
                new[] { "Root", "Leaf" }, 0.01d, 1d);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Result, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("PROFILER_DATA_UNAVAILABLE"));
        }

        [TestCase("other-bottom", "bottom-id", "normal-id")]
        [TestCase("bottom-id", "other-bottom", "normal-id")]
        [TestCase("bottom-id", "bottom-id", null)]
        public void InvestigationCompositionRejectsWrongLinkedIds(
            string leafBottomUpId, string resultBottomUpId, string markerSampleId)
        {
            var sources = InvestigationSources(ProfilerSchema.FrameSelf, ProfilerSchema.BottomUp,
                ProfilerSchema.MarkerTime, leafBottomUpId, resultBottomUpId, markerSampleId);

            var result = ProfilerAnalysis.ComposeInvestigation(sources.Leaf, sources.Bottom, sources.Marker,
                new[] { "Root", "Leaf" }, 0.01d, 1d);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Result, Is.Null);
            Assert.That(result.Error.Code, Is.EqualTo("PROFILER_DATA_UNAVAILABLE"));
        }

        [Test]
        public void StructuralFingerprintChangesWithHistoryStructureButNotInputOrdering()
        {
            var first = Frames((1, 10d), (2, 20d));
            var reversed = new List<ProfilerFrameRecord> { first[1], first[0] };
            var changed = Frames((1, 10d), (2, 21d));

            Assert.That(ProfilerFingerprint.Compute(first), Is.EqualTo(ProfilerFingerprint.Compute(reversed)));
            Assert.That(ProfilerFingerprint.Compute(first), Is.Not.EqualTo(ProfilerFingerprint.Compute(changed)));
        }

        [Test]
        public void StructuralFingerprintUsesTheHierarchyRootRatherThanTheLowestItemId()
        {
            var first = new[] { Frame(1, 10d, Thread("Main Thread",
                Sample(9, null, "Root", 0d, 10d, 1d),
                Sample(1, 9, "Child", 0d, 5d, 5d))) };
            var changedRoot = new[] { Frame(1, 10d, Thread("Main Thread",
                Sample(10, null, "Root", 0d, 10d, 1d),
                Sample(1, 10, "Child", 0d, 5d, 5d))) };

            Assert.That(ProfilerFingerprint.Compute(first), Is.Not.EqualTo(ProfilerFingerprint.Compute(changedRoot)));
        }

        [Test]
        public void StructuralFingerprintSeparatesDelimiterAndNewlineBearingThreadIdentityFields()
        {
            var delimiterLeft = FrameWithIdentity("a|b", "c");
            var delimiterRight = FrameWithIdentity("a", "b|c");
            var newlineLeft = FrameWithIdentity("a\nb", "c");
            var newlineRight = FrameWithIdentity("a", "b\nc");

            Assert.That(ProfilerFingerprint.Compute(new[] { delimiterLeft }),
                Is.Not.EqualTo(ProfilerFingerprint.Compute(new[] { delimiterRight })));
            Assert.That(ProfilerFingerprint.Compute(new[] { newlineLeft }),
                Is.Not.EqualTo(ProfilerFingerprint.Compute(new[] { newlineRight })));
            Assert.That(ProfilerFingerprint.Compute(new[] { delimiterLeft }),
                Is.Not.EqualTo(ProfilerFingerprint.Compute(new[] { newlineLeft })));
        }

        private static ProfilerFrameRecord FrameWithIdentity(string groupName, string threadName)
        {
            return Frame(1, 10d, new ProfilerThreadRecord(0, 100, groupName, threadName, true,
                new[] { Sample(1, null, "Root", 0d, 10d, 1d) }));
        }

        private static ProfilerFrameRecord AllocationFrame(int frameIndex, long parentBytes, long childBytes)
        {
            var normal = new[]
            {
                Sample(1, null, "Root", 0d, 10d, 1d, markerId: 10, rawSampleIndices: new[] { 100 }),
                Sample(2, 1, "Parent", 0d, 9d, 1d, markerId: 20, rawSampleIndices: new[] { 110 }),
                Sample(3, 2, "Child", 1d, 4d, 1d, markerId: 30, rawSampleIndices: new[] { 120 })
            };
            var raw = new List<ProfilerSampleRecord>
            {
                Sample(100, null, "Root", 0d, 10d, 1d, markerId: 10),
                Sample(110, 100, "Parent", 0d, 9d, 1d, markerId: 20)
            };
            if (parentBytes != 0)
                raw.Add(Sample(111, 110, "GC.Alloc", 0d, 0d, 0d,
                    parentBytes, 1, markerId: 99));
            raw.Add(Sample(120, 110, "Child", 1d, 4d, 1d, markerId: 30));
            if (childBytes != 0)
                raw.Add(Sample(121, 120, "GC.Alloc", 1d, 0d, 0d,
                    childBytes, 1, markerId: 99));
            var thread = new ProfilerThreadRecord(0, 100, "Fixture", "Main Thread", true,
                normal, raw.ToArray(), Array.Empty<ProfilerSampleRecord>());
            return Frame(frameIndex, 10d, thread);
        }

        private static string NormalId(int frameIndex, long epoch, int itemId)
        {
            return ProfilerOpaqueId.Encode(
                new ProfilerOpaqueIdPayload(1, epoch, ProfilerViewKind.Normal, frameIndex, 0, itemId));
        }

        private static string RawId(int frameIndex, long epoch, int itemId)
        {
            return ProfilerOpaqueId.Encode(
                new ProfilerOpaqueIdPayload(1, epoch, ProfilerViewKind.Raw, frameIndex, 0, itemId));
        }

        private static int EarlierOpaqueItem(int first, int second, ProfilerViewKind kind)
        {
            var firstId = ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, 3, kind, 4, 0, first));
            var secondId = ProfilerOpaqueId.Encode(new ProfilerOpaqueIdPayload(1, 3, kind, 4, 0, second));
            return StringComparer.Ordinal.Compare(firstId, secondId) < 0 ? first : second;
        }

        private static (BatihanDev.UnityCliCommands.Foundation.CommandResult<FrameSelfResult> Leaf,
            BatihanDev.UnityCliCommands.Foundation.CommandResult<BottomUpResult> Bottom,
            BatihanDev.UnityCliCommands.Foundation.CommandResult<MarkerTimeResult> Marker) InvestigationSources(
            string leafSchema, string bottomSchema, string markerSchema,
            string leafBottomUpId, string resultBottomUpId, string markerSampleId)
        {
            var thread = new ThreadIdentity(0, 100, "Fixture", "Main Thread");
            var row = new SelfTimeRow(leafBottomUpId, new[] { new[] { "Root", "Leaf" } }, 10d, 10d, 1);
            var leaf = BatihanDev.UnityCliCommands.Foundation.CommandResult<FrameSelfResult>.Success(
                leafSchema, new FrameSelfResult("session-a", 4, thread, new[] { row }, 1));
            var bottom = BatihanDev.UnityCliCommands.Foundation.CommandResult<BottomUpResult>.Success(
                bottomSchema, new BottomUpResult("session-a", 4, thread, resultBottomUpId, "Leaf",
                    new[] { new[] { "Root", "Leaf" } }, 10d, 10d, 1, 10d,
                    Array.Empty<SelfTimeRow>(), Array.Empty<SampleSummary>()));
            var marker = BatihanDev.UnityCliCommands.Foundation.CommandResult<MarkerTimeResult>.Success(
                markerSchema, new MarkerTimeResult("session-a", 4, thread, markerSampleId,
                    new[] { "Root", "Leaf" }, 10d, 10d, 1));
            return (leaf, bottom, marker);
        }

        private static List<ProfilerFrameRecord> Frames(params (int Index, double Ms)[] values)
        {
            var frames = new List<ProfilerFrameRecord>();
            foreach (var value in values)
                frames.Add(Frame(value.Index, value.Ms,
                    Thread("Main Thread", Sample(1, null, "Root", 0d, value.Ms, value.Ms))));
            return frames;
        }

        private static ProfilerFrameRecord Frame(int index, double ms, params ProfilerThreadRecord[] threads)
        {
            return new ProfilerFrameRecord(index, index * 1000000L, (long)(ms * 1000000d), threads);
        }

        private static ProfilerThreadRecord Thread(
            string name,
            params ProfilerSampleRecord[] samples)
        {
            return Thread(name, true, samples);
        }

        private static ProfilerThreadRecord Thread(
            string name,
            bool allocationDataAvailable,
            params ProfilerSampleRecord[] samples)
        {
            return Thread(name, samples, 0, allocationDataAvailable);
        }

        private static ProfilerThreadRecord Thread(
            string name,
            ProfilerSampleRecord[] samples,
            int threadIndex = 0,
            bool allocationDataAvailable = true)
        {
            return new ProfilerThreadRecord(
                threadIndex, (ulong)(100 + threadIndex), "Fixture", name, allocationDataAvailable, samples);
        }

        private static ProfilerSampleRecord Sample(
            int id,
            int? parent,
            string marker,
            double startMs,
            double totalMs,
            double selfMs,
            long allocationBytes = 0,
            long allocationCount = 0,
            IReadOnlyList<long> flowIds = null,
            int markerId = -1,
            IReadOnlyList<int> rawSampleIndices = null)
        {
            return new ProfilerSampleRecord(
                id, parent, marker, (long)(startMs * 1000000d), (long)(totalMs * 1000000d),
                (long)(selfMs * 1000000d), 1, allocationBytes, allocationCount, false,
                flowIds ?? Array.Empty<long>(), markerId, rawSampleIndices);
        }
    }
}
