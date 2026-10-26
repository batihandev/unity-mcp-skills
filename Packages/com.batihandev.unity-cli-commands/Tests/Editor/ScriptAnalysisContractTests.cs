using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class ScriptAnalysisContractTests
    {
        [SetUp] public void Setup() { SceneContextReferenceFixture.GetterCalls = 0; }
        [Test] public void InspectUsesDeclaredPublicMembersAndSerializationCandidateRules()
        {
            var r = ScriptAnalysisCommands.Inspect(typeof(ScriptAnalysisPlainFixture).FullName); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Kind, Is.EqualTo("Class")); Assert.That(r.Result.Fields.Any(x => x.Name == "Private"), Is.False);
            Assert.That(r.Result.Fields.Single(x => x.Name == "Public").SerializationCandidate, Is.True); Assert.That(r.Result.Fields.Where(x => x.Name == "Static" || x.Name == "ReadOnly" || x.Name == "Excluded").All(x => !x.SerializationCandidate), Is.True);
            Assert.That(r.Result.Methods.Any(x => x.Name == "Method"), Is.True); Assert.That(SceneContextReferenceFixture.GetterCalls, Is.Zero);
        }
        [Test] public void IncludePrivateActuallyIncludesFieldsPropertiesMethodsWithoutGetters()
        {
            var r = ScriptAnalysisCommands.Inspect(typeof(ScriptAnalysisPlainFixture).FullName, true); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Fields.Any(x => x.Name == "Private" && x.IsPrivate), Is.True); Assert.That(r.Result.Properties.Any(x => x.Name == "HiddenProperty" && x.IsPrivate), Is.True); Assert.That(r.Result.Methods.Any(x => x.Name == "HiddenMethod" && x.IsPrivate), Is.True); Assert.That(SceneContextReferenceFixture.GetterCalls, Is.Zero);
        }
        [Test] public void PrivateLifecycleCallbackAppearsEvenWhenPrivateMembersOmitted()
        {
            var r = ScriptAnalysisCommands.Inspect(typeof(SceneContextReferenceFixture).FullName); Assert.That(r.Ok, Is.True); Assert.That(r.Result.UnityCallbacks, Does.Contain("Awake")); Assert.That(r.Result.Methods.Any(x => x.Name == "Awake"), Is.False);
        }
        [Test] public void QualifiedCollisionResolvesWhileSimpleNameRefuses()
        {
            Assert.That(ScriptAnalysisCommands.Inspect("ScriptAnalysisCollisionFixture").Error.Code, Is.EqualTo("ANALYSIS_TYPE_INVALID")); var r = ScriptAnalysisCommands.Inspect("ScriptAnalysisCollisionOne.ScriptAnalysisCollisionFixture"); Assert.That(r.Ok, Is.True); Assert.That(r.Result.FullName, Is.EqualTo("ScriptAnalysisCollisionOne.ScriptAnalysisCollisionFixture"));
        }
        [Test] public void CaseInsensitiveSimpleNameResolvesUniqueType()
        {
            var r = ScriptAnalysisCommands.Inspect("scriptanalysisplainfixture"); Assert.That(r.Ok, Is.True); Assert.That(r.Result.FullName, Is.EqualTo(typeof(ScriptAnalysisPlainFixture).FullName));
        }
        [Test] public void InspectAndGraphKeepDistinctEligibility()
        {
            var name = typeof(Microsoft.ScriptAnalysisFixtures.ScriptAnalysisInspectOnlyFixture).FullName; Assert.That(ScriptAnalysisCommands.Inspect(name).Ok, Is.True); Assert.That(ScriptAnalysisCommands.Graph(name).Error.Code, Is.EqualTo("ANALYSIS_TYPE_INVALID"));
        }
        [Test] public void GraphCollectsArrayGenericInheritedPrivateSerializedAndPublicStaticEdges()
        {
            var r = ScriptAnalysisCommands.Graph(typeof(SceneContextGraphA).FullName, 1); Assert.That(r.Ok, Is.True);
            foreach (var field in new[] { "Array", "Generic", "Inherited", "PrivateSerialized", "PublicStatic" }) Assert.That(r.Result.Edges.Any(x => x.From == typeof(SceneContextGraphA).FullName && x.Field == field), Is.True, field);
            Assert.That(r.Result.Edges.Any(x => x.Field == "PrivateUnserialized" || x.Field == "PrivateStatic" || x.Field == "Self"), Is.False); Assert.That(r.Result.Edges.Single(x => x.From == typeof(SceneContextGraphA).FullName && x.Field == "Inherited").DeclaringType, Is.EqualTo(typeof(SceneContextGraphBase).FullName));
        }
        [Test] public void GraphBidirectionalHopClosureIncludesIncomingTypesAndExactEndpoints()
        {
            var r = ScriptAnalysisCommands.Graph(typeof(SceneContextGraphB).FullName, 1); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Scripts.Any(x => x.FullName == typeof(SceneContextGraphA).FullName && x.Hop == 1), Is.True); Assert.That(r.Result.Scripts.Any(x => x.FullName == typeof(SceneContextGraphC).FullName && x.Hop == 1), Is.True); Assert.That(r.Result.Scripts.Any(x => x.FullName == typeof(SceneContextGraphD).FullName), Is.False);
            Assert.That(r.Result.Edges.All(x => r.Result.Scripts.Any(n => n.FullName == x.From) && r.Result.Scripts.Any(n => n.FullName == x.To)), Is.True);
        }
        [Test] public void GraphZeroHopIncludesOnlyEntryAndDetailsOmissionIsExplicit()
        {
            var r = ScriptAnalysisCommands.Graph(typeof(SceneContextGraphA).FullName, 0, false); Assert.That(r.Ok, Is.True); Assert.That(r.Result.TotalScriptsReached, Is.EqualTo(1)); Assert.That(r.Result.Edges, Is.Empty); Assert.That(r.Result.Scripts.Single().Fields, Is.Null); Assert.That(r.Result.Scripts.Single().UnityCallbacks, Is.Null);
        }
        [Test] public void GraphCyclesHaveLabeledRemainderAndNoFabricatedSourcePath()
        {
            var r = ScriptAnalysisCommands.Graph(typeof(SceneContextGraphA).FullName); Assert.That(r.Ok, Is.True); Assert.That(r.Result.CyclicRemainder, Does.Contain(typeof(SceneContextGraphA).FullName)); Assert.That(r.Result.SuggestedReadOrder.Distinct().Count(), Is.EqualTo(r.Result.TotalScriptsReached)); Assert.That(r.Result.Scripts.Single(x => x.FullName == typeof(SceneContextGraphA).FullName).FilePath, Is.Null); Assert.That(r.Result.Scope, Does.Contain("field"));
        }
        [Test] public void GraphRefusesNegativeHopsMissingAndAmbiguousEntry()
        {
            Assert.That(ScriptAnalysisCommands.Graph(typeof(SceneContextGraphA).FullName,-1).Error.Code, Is.EqualTo("ANALYSIS_INPUT_INVALID")); Assert.That(ScriptAnalysisCommands.Graph("ScriptAnalysisCollisionFixture").Error.Code, Is.EqualTo("ANALYSIS_TYPE_INVALID")); Assert.That(ScriptAnalysisCommands.Inspect("Absent3388").Error.Code, Is.EqualTo("ANALYSIS_TYPE_INVALID"));
        }
    }
}
