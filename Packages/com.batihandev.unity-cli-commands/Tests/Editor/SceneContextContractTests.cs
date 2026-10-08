using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using BatihanDev.UnityCliCommands.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class SceneContextContractTests
    {
        private const string Folder = "Assets/__SceneContext3388";
        private UnityEngine.SceneManagement.Scene first;
        [SetUp] public void Setup()
        {
            first = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.That(AssetDatabase.IsValidFolder(Folder), Is.False); AssetDatabase.CreateFolder("Assets", "__SceneContext3388");
            Assert.That(EditorSceneManager.SaveScene(first, Folder + "/Base.unity"), Is.True); SceneContextReferenceFixture.GetterCalls = 0;
        }
        [TearDown] public void Cleanup() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); AssetDatabase.DeleteAsset(Folder); }
        private GameObject Go(string name, GameObject parent = null) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, first); if (parent != null) go.transform.SetParent(parent.transform); return go; }
        [Test] public void ContextSeparatesActiveBfsScopeFromAllLoadedTotal()
        {
            var root = Go("Root"); Go("Child", root); var other = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); var extra = new GameObject("Other"); SceneManager.MoveGameObjectToScene(extra, other); SceneManager.SetActiveScene(first);
            var r = SceneContextCommands.Context(); Assert.That(r.Ok, Is.True); Assert.That(r.Result.TotalObjects, Is.EqualTo(3)); Assert.That(r.Result.ScopeObjects, Is.EqualTo(2)); Assert.That(r.Result.ExportedObjects, Is.EqualTo(2)); Assert.That(r.Result.Truncated, Is.False); Assert.That(r.Result.Objects.Select(x => x.Name), Is.EqualTo(new[] { "Root", "Child" }));
        }
        [Test] public void ExactRootScopeIncludesInactiveAndReportsDepthVersusCountOmissions()
        {
            var root = Go("Root"); var child = Go("Child", root); child.SetActive(false); Go("Grandchild", child); Go("Other");
            var r = SceneContextCommands.Context(0, 10, ExactObjectReference.ExactId(root)); Assert.That(r.Ok, Is.True); Assert.That(r.Result.ScopeObjects, Is.EqualTo(3)); Assert.That(r.Result.ExportedObjects, Is.EqualTo(1)); Assert.That(r.Result.DepthOmitted, Is.EqualTo(2)); Assert.That(r.Result.CountOmitted, Is.Zero);
            var cap = SceneContextCommands.Context(10, 1, ExactObjectReference.ExactId(root)); Assert.That(cap.Result.CountOmitted, Is.EqualTo(2)); Assert.That(cap.Result.DepthOmitted, Is.Zero);
        }
        [Test] public void ValuesUseSerializedJsonAndScalarFactsWithoutExecutingGetters()
        {
            var go = Go("Source"); var fixture = go.AddComponent<SceneContextReferenceFixture>(); var r = SceneContextCommands.Context(includeValues:true, includeCodeDeps:true); Assert.That(r.Ok, Is.True);
            var component = r.Result.Objects.Single().Components.Single(x => x.Type == typeof(SceneContextReferenceFixture).FullName);
            Assert.That(component.SerializedJson, Does.Contain("37")); Assert.That(component.Values.Single(x => x.Path == "Number").Value, Is.EqualTo("37")); Assert.That(component.Values.Any(x => x.Path == "Offset" && !x.Supported), Is.True); Assert.That(SceneContextReferenceFixture.GetterCalls, Is.Zero);
            Assert.That(r.Result.CodeDependencies.Any(x => x.Field == "ArrayDependency"), Is.True);
        }
        [Test] public void OmittedSectionsDifferFromCollectedEmptySections()
        {
            Go("Plain"); var omitted = SceneContextCommands.Context(includeReferences:false); Assert.That(omitted.Ok, Is.True); Assert.That(omitted.Result.References, Is.Null); Assert.That(omitted.Result.CodeDependencies, Is.Null); Assert.That(omitted.Result.Objects.Single().Components.Single().Values, Is.Null);
            var requested = SceneContextCommands.Context(includeReferences:true, includeCodeDeps:true); Assert.That(requested.Ok, Is.True); Assert.That(requested.Result.References, Is.Not.Null); Assert.That(requested.Result.CodeDependencies, Is.Empty);
        }
        [Test] public void SerializedReferencesCarryActualObjectComponentAssetAndCrossSceneIdentities()
        {
            var source = Go("Source"); var fixture = source.AddComponent<SceneContextReferenceFixture>(); var target = Go("Target"); var light = target.AddComponent<Light>(); fixture.Assigned = target; fixture.OtherAssigned = light;
            var other = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); var outside = new GameObject("Target"); SceneManager.MoveGameObjectToScene(outside, other); outside.AddComponent<SceneContextReferenceFixture>().Assigned = target;
            SceneManager.SetActiveScene(first); var r = SceneContextCommands.Context(root:ExactObjectReference.ExactId(source)); Assert.That(r.Ok, Is.True);
            Assert.That(r.Result.References.Single(x => x.Property == "Assigned").Target, Is.EqualTo(ExactObjectReference.ExactId(target)));
            Assert.That(r.Result.References.Single(x => x.Property == "OtherAssigned").TargetObject, Is.EqualTo(ExactObjectReference.ExactId(target)));
            Assert.That(r.Result.References.All(x => x.SourceObject == ExactObjectReference.ExactId(source)), Is.True);
            var asset = UnityEngine.ScriptableObject.CreateInstance<ScriptableObjectFlagsFixtureAsset>(); AssetDatabase.CreateAsset(asset, Folder + "/Asset.asset"); fixture.Assigned = asset;
            var assetEdge = SceneContextCommands.Context(root:ExactObjectReference.ExactId(source)).Result.References.Single(x => x.Property == "Assigned"); Assert.That(assetEdge.TargetAssetPath, Is.EqualTo(Folder + "/Asset.asset")); Assert.That(assetEdge.TargetScenePath, Is.Null.Or.Empty);
        }
        [Test] public void DependencyImpactClassifiesComponentTargetsByOwningObject()
        {
            var root = Go("Root"); var child = Go("Child", root); var external = Go("External"); var light = child.AddComponent<Light>();
            external.AddComponent<SceneContextReferenceFixture>().Assigned = light; root.AddComponent<SceneContextReferenceFixture>().Assigned = external; child.AddComponent<SceneContextReferenceFixture>().Assigned = root;
            var r = SceneContextCommands.Dependencies(ExactObjectReference.ExactId(root)); Assert.That(r.Ok, Is.True);
            Assert.That(r.Result.Incoming.Any(x => x.SourceObject == ExactObjectReference.ExactId(external) && x.Property == "Assigned"), Is.True);
            Assert.That(r.Result.Outgoing.Any(x => x.TargetObject == ExactObjectReference.ExactId(external)), Is.True);
            Assert.That(r.Result.Internal.Any(x => x.SourceObject == ExactObjectReference.ExactId(child) && x.Property == "Assigned"), Is.True);
            Assert.That(r.Result.Markdown, Does.Contain("Assigned")); Assert.That(r.Result.Scope, Does.Contain("serialized"));
        }
        [Test] public void ContextDistinguishesLegitimateNullFromActualMissingReference()
        {
            var fixture = Go("Source").AddComponent<SceneContextReferenceFixture>(); var target = Go("Removed"); fixture.Assigned = target; UnityObject.DestroyImmediate(target);
            var r = SceneContextCommands.Context(); Assert.That(r.Ok, Is.True); Assert.That(r.Result.References.Single(x => x.Property == "Assigned").Missing, Is.True); Assert.That(r.Result.References.Single(x => x.Property == "LegitimateNull").Null, Is.True);
        }
        [Test] public void MissingComponentIsExplicitAndDoesNotEraseOtherComponentFacts()
        {
            var go = Go("Broken"); var fixture = go.AddComponent<SceneContextReferenceFixture>(); var so = new SerializedObject(fixture); so.FindProperty("m_Script").objectReferenceValue = null; so.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go), Is.EqualTo(1)); var r = SceneContextCommands.Context(includeValues:true); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Objects.Single().Components.Any(x => x.Missing), Is.True);
        }
        [Test] public void ReportPopulatesHierarchyFieldsBothGraphsAndCountsAttachedInstances()
        {
            var go = Go("Export"); var fixture = go.AddComponent<SceneContextReferenceFixture>(); go.AddComponent<SceneContextReferenceFixture>(); fixture.Assigned = Go("Target");
            var r = SceneContextCommands.Report(); Assert.That(r.Ok, Is.True); Assert.That(r.Result.UserScriptCount, Is.EqualTo(2)); Assert.That(r.Result.Markdown, Does.Contain("Hierarchy")); Assert.That(r.Result.Markdown, Does.Contain("Script Fields")); Assert.That(r.Result.Markdown, Does.Contain("Dependency Graph")); Assert.That(r.Result.Markdown, Does.Contain("Number")); Assert.That(r.Result.ReferenceCount, Is.GreaterThan(0)); Assert.That(r.Result.CodeReferenceCount, Is.GreaterThan(0));
            Assert.That(r.Result.Context.Objects.Single(x => x.Name == "Export").Components.Where(x => x.Type == typeof(SceneContextReferenceFixture).FullName).All(x => x.SourceClassification == "ProjectOwned"), Is.True);
        }
        [Test] public void ContextAndReportDoNotSaveImportDirtyOrChangeSelection()
        {
            var fixture = Go("ReadOnly").AddComponent<SceneContextReferenceFixture>(); Selection.activeGameObject = fixture.gameObject; EditorSceneManager.SaveScene(first); var before = EditorJsonUtility.ToJson(fixture); var files = System.IO.Directory.GetFiles(Folder).OrderBy(x => x).ToArray();
            var r = SceneContextCommands.Report(); Assert.That(r.Ok, Is.True); Assert.That(SceneContextCommands.Dependencies().Ok, Is.True); Assert.That(first.isDirty, Is.False); Assert.That(Selection.activeGameObject, Is.SameAs(fixture.gameObject)); Assert.That(EditorJsonUtility.ToJson(fixture), Is.EqualTo(before)); Assert.That(System.IO.Directory.GetFiles(Folder).OrderBy(x => x), Is.EqualTo(files)); Assert.That(SceneContextReferenceFixture.GetterCalls, Is.Zero);
        }
        [Test] public void SnapshotReportsActualSessionAndTracksAllSixChangeFields()
        {
            var go = Go("Before"); var capture = SceneContextCommands.Diff(); Assert.That(capture.Ok, Is.True); Assert.That(capture.Result.Snapshot.Version, Is.EqualTo(1)); Assert.That(capture.Result.Snapshot.EditorSession, Is.Not.Empty); Assert.That(capture.Result.Snapshot.Objects.Single().Target, Is.EqualTo(ExactObjectReference.ExactId(go)));
            go.name = "After"; go.AddComponent<Light>(); go.transform.position = Vector3.right; go.transform.eulerAngles = new Vector3(0, 30, 0); go.transform.localScale = Vector3.one * 2;
            var r = SceneContextCommands.Diff(capture.Result.SnapshotJson); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Modified.Single().Changes, Is.EquivalentTo(new[] { "name", "path", "components", "position", "rotation", "scale" }));
        }
        [Test] public void SnapshotTracksAddedRemovedLoadedSceneObjects()
        {
            var removed = Go("Removed"); var capture = SceneContextCommands.Diff(); Assert.That(capture.Ok, Is.True); UnityObject.DestroyImmediate(removed); Go("Added"); var r = SceneContextCommands.Diff(capture.Result.SnapshotJson); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Added.Single().Name, Is.EqualTo("Added")); Assert.That(r.Result.Removed.Single().Name, Is.EqualTo("Removed"));
        }
        [Test] public void SnapshotFloatComparisonUsesStrictAxisEpsilon()
        {
            var go = Go("Tolerance"); var capture = SceneContextCommands.Diff(); Assert.That(capture.Ok, Is.True); go.transform.position = new Vector3(0.0001f,0,0); Assert.That(SceneContextCommands.Diff(capture.Result.SnapshotJson).Result.Modified, Is.Empty); go.transform.position = new Vector3(0.00011f,0,0); Assert.That(SceneContextCommands.Diff(capture.Result.SnapshotJson).Result.Modified.Single().Changes, Does.Contain("position"));
        }
        [TestCase("{")] [TestCase("{}")] [TestCase("[]")] [TestCase("{\"Version\":2,\"EditorSession\":\"other\",\"Objects\":[]}")]
        public void SnapshotRefusesMalformedOrUnsupportedEnvelope(string json) { Assert.That(SceneContextCommands.Diff(json).Error.Code, Is.EqualTo("ANALYSIS_SNAPSHOT_INVALID")); }
        [Test] public void SnapshotRefusesCrossProcessIdentity()
        {
            Go("One"); var capture = SceneContextCommands.Diff(); Assert.That(capture.Ok, Is.True); var json = capture.Result.SnapshotJson.Replace(capture.Result.Snapshot.EditorSession, "not-this-process"); Assert.That(SceneContextCommands.Diff(json).Error.Code, Is.EqualTo("ANALYSIS_SNAPSHOT_SESSION"));
        }
        [Test] public void SnapshotRefusesDuplicateZeroAndMissingObjectIds()
        {
            var go = Go("One"); var capture = SceneContextCommands.Diff(); Assert.That(capture.Ok, Is.True);
            var id = ExactObjectReference.ExactId(go); var malformed = capture.Result.SnapshotJson.Replace(id, "entity:0"); Assert.That(SceneContextCommands.Diff(malformed).Ok, Is.False);
            var missing = capture.Result.SnapshotJson.Replace(id, ""); Assert.That(SceneContextCommands.Diff(missing).Ok, Is.False);
        }
        [Test] public void SnapshotRefusesDuplicateIdsAndNonfiniteCoordinates()
        {
            var go = Go("One"); var capture = SceneContextCommands.Diff(); Assert.That(capture.Ok, Is.True);
            var session = capture.Result.Snapshot.EditorSession; var id = ExactObjectReference.ExactId(go);
            var entry = "{\"Target\":\"" + id + "\",\"Name\":\"One\",\"Path\":\"One\",\"ScenePath\":\"" + Folder + "/Base.unity\",\"Components\":[\"UnityEngine.Transform\"],\"Position\":[0,0,0],\"Rotation\":[0,0,0],\"Scale\":[1,1,1]}";
            var envelope = "{\"Version\":1,\"EditorSession\":\"" + session + "\",\"Objects\":[";
            Assert.That(SceneContextCommands.Diff(envelope + entry + "," + entry + "]}").Error.Code, Is.EqualTo("ANALYSIS_SNAPSHOT_INVALID"));
            Assert.That(SceneContextCommands.Diff(envelope + entry.Replace("[0,0,0]", "[NaN,0,0]") + "]}").Error.Code, Is.EqualTo("ANALYSIS_SNAPSHOT_INVALID"));
        }
        [Test] public void BoundsAndExactRootRefuseRatherThanWidenScope()
        {
            Assert.That(SceneContextCommands.Context(-1).Error.Code, Is.EqualTo("ANALYSIS_INPUT_INVALID")); Assert.That(SceneContextCommands.Context(maxObjects:-1).Error.Code, Is.EqualTo("ANALYSIS_INPUT_INVALID")); Assert.That(SceneContextCommands.Context(root:"entity:0").Ok, Is.False); Assert.That(SceneContextCommands.Dependencies("entity:0").Ok, Is.False);
        }
    }
}
