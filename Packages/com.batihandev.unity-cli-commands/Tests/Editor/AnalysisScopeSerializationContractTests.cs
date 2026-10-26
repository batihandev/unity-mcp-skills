using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class AnalysisScopeSerializationContractTests : SmartFixtureSupport
    {
        [TestCase("snap")] [TestCase("layout")] [TestCase("random")] [TestCase("distribute")] [TestCase("ground")]
        public void TransformCommandsSerializeEveryPoseFieldAsNumericData(string operation)
        {
            var a = Go("A", new Vector3(1, 2, 3)); var b = Go("B", new Vector3(8, 9, 10)); var c = Go("C", new Vector3(11, 12, 13));
            var targets = Targets(a, b, c);
            var result = operation == "snap" ? SmartTransformCommands.SnapGrid(targetsJson: targets) : operation == "layout" ? SmartTransformCommands.Layout(targetsJson: targets) : operation == "random" ? SmartTransformCommands.RandomTransform(targetsJson: targets) : operation == "distribute" ? SmartTransformCommands.Distribute(targetsJson: targets) : SmartTransformCommands.AlignGround(targetsJson: targets);
            Assert.That(result.Ok, Is.True);
            var json = JsonConvert.SerializeObject(result);
            var item = JObject.Parse(json)["Result"]["Items"][0];
            foreach (var field in new[] { "OriginalPosition", "ProposedPosition", "OriginalScale", "ProposedScale", "Normal" }) Shape(item[field], "X", "Y", "Z");
            foreach (var field in new[] { "OriginalRotation", "ProposedRotation" }) Shape(item[field], "X", "Y", "Z", "W");
            Assert.That((float)item["OriginalPosition"]["X"], Is.EqualTo(1)); Assert.That((float)item["OriginalPosition"]["Y"], Is.EqualTo(2)); Assert.That((float)item["OriginalPosition"]["Z"], Is.EqualTo(3));
            Assert.That(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<BatihanDev.UnityCliCommands.Foundation.CommandResult<SmartTransformReport>>(json)), Is.EqualTo(json));
        }
        [Test] public void SpatialCommandSerializesCenterAsNumericData()
        {
            var result = SmartQueryCommands.Spatial(1, 2, 3); Assert.That(result.Ok, Is.True);
            var json = JsonConvert.SerializeObject(result); var center = JObject.Parse(json)["Result"]["Center"]; Shape(center, "X", "Y", "Z");
            Assert.That(((JObject)center).Properties().Select(property => (float)property.Value), Is.EqualTo(new[] { 1f, 2f, 3f }));
            Assert.That(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<BatihanDev.UnityCliCommands.Foundation.CommandResult<SmartSpatialReport>>(json)), Is.EqualTo(json));
        }
        private static void Shape(JToken value, params string[] keys)
        { Assert.That(value, Is.TypeOf<JObject>()); Assert.That(((JObject)value).Properties().Select(p => p.Name), Is.EqualTo(keys)); Assert.That(value.Children<JProperty>().All(p => p.Value.Type == JTokenType.Float || p.Value.Type == JTokenType.Integer), Is.True); }
        [TestCase(0, true)] [TestCase(1, true)] [TestCase(2, true)] [TestCase(4, false)] [TestCase(8, true)] [TestCase(16, true)] [TestCase(32, true)] [TestCase(52, false)]
        public void ValidationUsesNativeGameObjectEligibilityAndDeliberateAttachedComponents(int flags, bool nativeEligible)
        {
            var go = Go("Flagged"); go.hideFlags = (HideFlags)flags;
            try
            {
                var native = UnityEngine.Object.FindObjectsOfType<GameObject>(); Assert.That(native.Contains(go), Is.EqualTo(nativeEligible), "Unity native scope prerequisite");
                var report = ValidationDiagnosticsCommands.Scene(false, false, false, true); Assert.That(report.Ok, Is.True);
                Assert.That(report.Result.Findings.Any(f => f.Target == Id(go)), Is.EqualTo(nativeEligible));
            } finally { go.hideFlags = HideFlags.None; }
        }
        [TestCase(1)] [TestCase(2)] [TestCase(4)] [TestCase(8)] [TestCase(16)] [TestCase(32)] [TestCase(52)]
        public void PerceptionContextAndSnapshotsKeepHiddenInactiveHierarchy(int flags)
        {
            var root = Go("Hidden"); var child = Go("Inactive", parent:root); child.SetActive(false); root.hideFlags = (HideFlags)flags;
            try
            {
                var summary = ScenePerceptionCommands.Summary(); Assert.That(summary.Ok, Is.True); Assert.That(summary.Result.Stats.TotalObjects, Is.EqualTo(2));
                var tags = ScenePerceptionCommands.TagLayers(); Assert.That(tags.Ok, Is.True); Assert.That(tags.Result.TotalObjects, Is.EqualTo(2));
                var spatial = ScenePerceptionCommands.Spatial(radius:1); Assert.That(spatial.Ok, Is.True); Assert.That(spatial.Result.Results.Select(r => r.Target), Is.EqualTo(new[] { Id(root), Id(child) }));
                var hotspots = ScenePerceptionCommands.Hotspots(0, 1, 100); Assert.That(hotspots.Ok, Is.True); Assert.That(hotspots.Result.Findings.Select(f => f.Target), Does.Contain(Id(root)));
                var context = SceneContextCommands.Context(root:Id(root)); Assert.That(context.Ok, Is.True); Assert.That(context.Result.TotalObjects, Is.EqualTo(2)); Assert.That(context.Result.Objects.Select(r => r.Target), Is.EqualTo(new[] { Id(root), Id(child) }));
                var dependencies = SceneContextCommands.Dependencies(Id(root)); Assert.That(dependencies.Ok, Is.True); Assert.That(dependencies.Result.ObjectsAnalyzed, Is.EqualTo(2));
                var diff = SceneContextCommands.Diff(); Assert.That(diff.Ok, Is.True); Assert.That(diff.Result.Snapshot.Objects.Select(r => r.Target), Is.EqualTo(new[] { Id(root), Id(child) }));
            } finally { root.hideFlags = HideFlags.None; }
        }
        [TestCase(1)] [TestCase(2)] [TestCase(8)] [TestCase(16)] [TestCase(32)]
        public void BindingUsesNativeTagFirstNameSecondAndUnfilteredComponentConversion(int flags)
        {
            var target = Go("Target").AddComponent<SmartBindingFixture>(); var named = Go("SourceNamed").AddComponent<Light>(); var tagged = Go("SourceTagged").AddComponent<Light>(); tagged.gameObject.tag = "Player"; named.gameObject.hideFlags = (HideFlags)flags; tagged.gameObject.hideFlags = (HideFlags)flags; named.hideFlags = HideFlags.DontSaveInEditor;
            try
            {
                var native = GameObject.FindGameObjectsWithTag("Player").Concat(UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Where(go => go.name.Contains("Source"))).Distinct().Select(go => go.GetComponent<Light>()).Where(light => light != null).Select(Id).ToArray();
                Assert.That(native, Does.Contain(Id(named))); Assert.That(native[0], Is.EqualTo(Id(tagged)));
                var result = SmartBindingCommands.ReferencePlan(null, null, "Lights", "Player", "Source", target:Id(target)); Assert.That(result.Ok, Is.True); Assert.That(result.Result.ProposedReferences, Is.EqualTo(native));
            } finally { named.gameObject.hideFlags = HideFlags.None; tagged.gameObject.hideFlags = HideFlags.None; named.hideFlags = HideFlags.None; }
        }
        [Test] public void HiddenContextReferenceSourcesRetainActualMissingMarkers()
        {
            var source = Go("HiddenSource"); var fixture = source.AddComponent<SceneContextReferenceFixture>(); var removed = Go("Removed"); fixture.Assigned = removed; UnityEngine.Object.DestroyImmediate(removed); source.hideFlags = HideFlags.HideInHierarchy;
            try
            {
                using (var serialized = new UnityEditor.SerializedObject(fixture)) { var assigned = serialized.FindProperty("Assigned"); Assert.That(assigned.objectReferenceValue, Is.Null); Assert.That(assigned.objectReferenceEntityIdValue, Is.Not.EqualTo(default(EntityId))); Assert.That(serialized.FindProperty("LegitimateNull").objectReferenceEntityIdValue, Is.EqualTo(default(EntityId))); }
                var context = SceneContextCommands.Context(root:Id(source)); Assert.That(context.Ok, Is.True); var missing = context.Result.References.Single(r => r.Property == "Assigned"); Assert.That(missing.Missing, Is.True); Assert.That(missing.Null, Is.False); Assert.That(context.Result.References.Single(r => r.Property == "LegitimateNull").Null, Is.True);
                var health = ScenePerceptionCommands.Health(); Assert.That(health.Ok, Is.True); Assert.That(health.Result.Findings.Any(f => f.Target == Id(source) && f.Kind == "MissingReference"), Is.True);
            } finally { source.hideFlags = HideFlags.None; }
        }
        [Test] public void MissingDefaultKeepsVisibleManualScopeWhileActiveModeUsesNativeScope()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.batihandev.unity-cli-commands/Tests/Fixtures/MissingScript.prefab"); Assert.That(prefab, Is.Not.Null);
            var visible = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, First); var hidden = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, First); hidden.hideFlags = HideFlags.HideInHierarchy;
            try
            {
                Assert.That(UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(hidden), Is.GreaterThan(0));
                var defaults = SceneAnalysisCommands.Missing(); Assert.That(defaults.Ok, Is.True); Assert.That(defaults.Result.Issues.Select(i => i.Target), Does.Contain(Id(visible))); Assert.That(defaults.Result.Issues.Select(i => i.Target), Does.Not.Contain(Id(hidden)));
                var native = SceneAnalysisCommands.Missing(false); Assert.That(native.Ok, Is.True); Assert.That(native.Result.Issues.Select(i => i.Target), Does.Contain(Id(hidden)));
                var fix = MissingScriptCommands.Fix(false); Assert.That(fix.Ok, Is.True); Assert.That(fix.Result.Targets, Does.Contain(Id(hidden)));
            } finally { hidden.hideFlags = HideFlags.None; }
        }
        [Test] public void LodUsesNativeInactiveQueriedRootRendererScope()
        {
            var root = Go("InactiveLod"); root.AddComponent<MeshRenderer>(); Go("Child", parent:root).AddComponent<MeshRenderer>(); root.SetActive(false);
            var native = root.GetComponentsInChildren<Renderer>(); Assert.That(native.Length, Is.EqualTo(2), "Native inactive-root query includes both root and child on the supported Editor"); Assert.That(native.Select(r => r.gameObject.name), Is.EquivalentTo(new[] { "InactiveLod", "Child" }));
            var result = SceneOptimizationCommands.LodSetup(Id(root), dryRun:true); Assert.That(result.Ok, Is.True); Assert.That(result.Result.RendererCount, Is.EqualTo(native.Length));
        }
        [Test] public void PhysicsSpatialPreservesNativeHiddenColliderOccurrencesAndOrder()
        {
            var go = Go("HiddenPhysics"); go.AddComponent<BoxCollider>(); go.AddComponent<SphereCollider>(); go.hideFlags = HideFlags.HideInHierarchy;
            try { Physics.SyncTransforms(); var native = Physics.OverlapSphere(Vector3.zero, 5).Where(c => c.gameObject.scene == First).Select(Id).ToArray(); Assert.That(native.Length, Is.EqualTo(2)); var result = SmartQueryCommands.Spatial(0,0,0,5); Assert.That(result.Ok, Is.True); Assert.That(result.Result.Items.Select(r => r.Collider), Is.EqualTo(native)); } finally { go.hideFlags = HideFlags.None; }
        }
    }
}
