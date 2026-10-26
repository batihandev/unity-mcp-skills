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
    public sealed class ScenePerceptionContractTests
    {
        private const string Folder = "Assets/__ScenePerception3388";
        private UnityEngine.SceneManagement.Scene first;
        private Material material;
        [SetUp] public void Setup()
        {
            first = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.That(AssetDatabase.IsValidFolder(Folder), Is.False, "Fixture path must be unowned before setup.");
            AssetDatabase.CreateFolder("Assets", "__ScenePerception3388");
            Assert.That(EditorSceneManager.SaveScene(first, Folder + "/Base.unity"), Is.True);
            material = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        [TearDown] public void Cleanup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (material != null) UnityObject.DestroyImmediate(material);
            AssetDatabase.DeleteAsset(Folder);
        }
        private GameObject Go(string name, GameObject parent = null)
        {
            var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, first);
            if (parent != null) go.transform.SetParent(parent.transform);
            return go;
        }
        [Test] public void SummarySeparatesActiveRootsFromAllLoadedAndRefreshesBetweenRequests()
        {
            Go("First").AddComponent<Light>(); var inactive = Go("Inactive"); inactive.SetActive(false);
            var extra = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var go = new GameObject("Other"); SceneManager.MoveGameObjectToScene(go, extra); SceneManager.SetActiveScene(first);
            var r = ScenePerceptionCommands.Summary(); Assert.That(r.Ok, Is.True);
            Assert.That(r.Result.Stats.TotalObjects, Is.EqualTo(3)); Assert.That(r.Result.Stats.RootObjects, Is.EqualTo(2));
            Assert.That(r.Result.Stats.ActiveObjects, Is.EqualTo(2)); Assert.That(r.Result.Stats.InactiveObjects, Is.EqualTo(1));
            Assert.That(r.Result.TopComponents.Any(x => x.Name == "Transform"), Is.False);
            Go("Later").AddComponent<Light>(); Assert.That(ScenePerceptionCommands.Summary().Result.Stats.TotalObjects, Is.EqualTo(4));
        }
        [Test] public void SummaryIncludesHiddenHierarchyAndExcludesPreviewAndPersistentObjects()
        {
            var ordinary = Go("Ordinary"); var hidden = Go("Hidden"); hidden.hideFlags = HideFlags.HideInHierarchy;
            var persistentSource = Go("PersistentSource"); var prefab = PrefabUtility.SaveAsPrefabAsset(persistentSource, Folder + "/Persistent.prefab"); UnityObject.DestroyImmediate(persistentSource);
            Assert.That(EditorUtility.IsPersistent(prefab), Is.True);
            var preview = EditorSceneManager.NewPreviewScene(); var previewGo = new GameObject("Preview"); SceneManager.MoveGameObjectToScene(previewGo, preview);
            try
            {
                Assert.That(hidden.scene.IsValid() && hidden.scene.isLoaded && first.GetRootGameObjects().Contains(hidden), Is.True, "Hidden fixture must remain an ordinary loaded scene member after prefab import.");
                var expected = new[] { ExactObjectReference.ExactId(ordinary), ExactObjectReference.ExactId(hidden) };
                var r = ScenePerceptionCommands.Summary(); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Stats.TotalObjects, Is.EqualTo(2));
                var spatial = ScenePerceptionCommands.Spatial(radius:1); Assert.That(spatial.Ok, Is.True); Assert.That(spatial.Result.Results.Select(row => row.Target), Is.EqualTo(expected));
                Assert.That(spatial.Result.Results.Select(row => row.Target), Does.Not.Contain(ExactObjectReference.ExactId(previewGo))); Assert.That(spatial.Result.Results.Select(row => row.Target), Does.Not.Contain(ExactObjectReference.ExactId(prefab)));
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); UnityObject.DestroyImmediate(hidden); }
        }
        [Test] public void SummaryTopZeroAndOmittedStatsHaveDistinctResults()
        {
            Go("Light").AddComponent<Light>(); var capped = ScenePerceptionCommands.Summary(true, 0); Assert.That(capped.Ok, Is.True); Assert.That(capped.Result.TopComponents, Is.Empty);
            var omitted = ScenePerceptionCommands.Summary(false); Assert.That(omitted.Ok, Is.True); Assert.That(omitted.Result.TopComponents, Is.Null);
        }
        [Test] public void HotspotsPreserveInclusiveThresholdAndSeverityBoundaries()
        {
            var root = Go("Root"); var a = Go("A", root); Go("B", a); Go("C", a);
            var r = ScenePerceptionCommands.Hotspots(2, 2, 100); Assert.That(r.Ok, Is.True);
            Assert.That(r.Result.Findings.Count(x => x.Kind == "DeepHierarchy"), Is.EqualTo(2));
            Assert.That(r.Result.Findings.Single(x => x.Kind == "LargeChildSet").Severity, Is.EqualTo("Info"));
            Go("D", a); Go("E", a); Assert.That(ScenePerceptionCommands.Hotspots(20, 2).Result.Findings.Single(x => x.Kind == "LargeChildSet").Severity, Is.EqualTo("Warning"));
        }
        [Test] public void EmptyClustersSeparateSamePathsAcrossScenesAndGroupByExactParent()
        {
            var parent = Go("Parent"); Go("A", parent); Go("B", parent); Go("C", parent);
            var other = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var p = new GameObject("Parent"); SceneManager.MoveGameObjectToScene(p, other);
            new GameObject("D").transform.SetParent(p.transform); new GameObject("E").transform.SetParent(p.transform);
            var r = ScenePerceptionCommands.Hotspots(100, 100); Assert.That(r.Ok, Is.True);
            Assert.That(r.Result.Findings.Count(x => x.Kind == "EmptyLeafCluster"), Is.EqualTo(1));
            Assert.That(r.Result.Findings.Single(x => x.Kind == "EmptyLeafCluster").Count, Is.EqualTo(3));
        }
        [Test] public void HotspotDuplicateNameIsExactCaseAndWarningAtFive()
        {
            for (var i = 0; i < 5; i++) Go("Same"); Go("same");
            var r = ScenePerceptionCommands.Hotspots(100, 100, 1); Assert.That(r.Ok, Is.True);
            Assert.That(r.Result.Findings.Single().Kind, Is.EqualTo("DuplicateNameCluster")); Assert.That(r.Result.Findings.Single().Count, Is.EqualTo(5)); Assert.That(r.Result.Findings.Single().Severity, Is.EqualTo("Warning")); Assert.That(r.Result.Truncated, Is.True);
        }
        [Test] public void HealthZeroCapRetainsCompleteSeverityTotalsAndNoShownSuggestions()
        {
            Go("One"); var r = ScenePerceptionCommands.Health(0); Assert.That(r.Ok, Is.True);
            Assert.That(r.Result.Total, Is.EqualTo(2)); Assert.That(r.Result.Errors, Is.EqualTo(1)); Assert.That(r.Result.Warnings, Is.EqualTo(1)); Assert.That(r.Result.ShownErrors, Is.Zero); Assert.That(r.Result.Findings, Is.Empty); Assert.That(r.Result.SuggestedGuides, Is.Empty); Assert.That(r.Result.Truncated, Is.True);
        }
        [Test] public void HealthRetainsDuplicateHotspotButExcludesDuplicateFinding()
        {
            Go("Same"); Go("Same"); var r = ScenePerceptionCommands.Health(); Assert.That(r.Ok, Is.True);
            Assert.That(r.Result.Hotspots.Any(x => x.Kind == "DuplicateNameCluster"), Is.True); Assert.That(r.Result.Findings.Any(x => x.Kind == "DuplicateNameCluster"), Is.False);
        }
        [Test] public void ContractChecksActiveSceneRootsAndActualTagsLayers()
        {
            Go("Systems"); var r = ScenePerceptionCommands.Contract(null, "[\"__AbsentTag3388\"]", "[\"__AbsentLayer3388\"]"); Assert.That(r.Ok, Is.True);
            Assert.That(r.Result.CheckedRoots, Is.EqualTo(new[] { "Systems", "Managers", "Gameplay", "UIRoot" }));
            Assert.That(r.Result.Findings.Count(x => x.Kind == "MissingRoot"), Is.EqualTo(3)); Assert.That(r.Result.Findings.Any(x => x.Kind == "MissingTagDefinition"), Is.True); Assert.That(r.Result.Findings.Any(x => x.Kind == "MissingLayerDefinition"), Is.True); Assert.That(r.Result.Passed, Is.False);
        }
        [Test] public void ContractEventSystemToggleDoesNotDisableCameraAndLightConventions()
        {
            Go("Canvas").AddComponent<Canvas>(); var r = ScenePerceptionCommands.Contract("[]", requireEventSystemForUi:false); Assert.That(r.Ok, Is.True);
            Assert.That(r.Result.Findings.Any(x => x.Kind == "MissingEventSystem"), Is.False); Assert.That(r.Result.Findings.Any(x => x.Kind == "MissingMainCamera"), Is.True);
            Assert.That(ScenePerceptionCommands.Contract("[]").Result.Findings.Any(x => x.Kind == "MissingEventSystem"), Is.True);
        }
        [TestCase("{")] [TestCase("[1]")] [TestCase("[\"A\",]")]
        public void ContractRefusesMalformedArrays(string json) { Assert.That(ScenePerceptionCommands.Contract(json).Error.Code, Is.EqualTo("ANALYSIS_INPUT_INVALID")); }
        [Test] public void TagLayersCountInactiveAndListUnusedDefinedLayers()
        {
            Go("A"); Go("B").SetActive(false); var r = ScenePerceptionCommands.TagLayers(); Assert.That(r.Ok, Is.True);
            Assert.That(r.Result.TotalObjects, Is.EqualTo(2)); Assert.That(r.Result.UntaggedCount, Is.EqualTo(2)); Assert.That(r.Result.Layers.Single().Name, Is.EqualTo("Default")); Assert.That(r.Result.EmptyDefinedLayers, Does.Contain("Ignore Raycast"));
        }
        [Test] public void SpatialUsesTransformsIncludesInactiveNoColliderAndChoosesNearestOnlyOverCap()
        {
            var far = Go("Far"); far.transform.position = new Vector3(9, 0, 0); var near = Go("Near"); near.transform.position = Vector3.right; near.SetActive(false);
            var full = ScenePerceptionCommands.Spatial(); Assert.That(full.Ok, Is.True); Assert.That(full.Result.Results.Select(x => x.Name), Is.EqualTo(new[] { "Far", "Near" }));
            var cap = ScenePerceptionCommands.Spatial(maxResults:1); Assert.That(cap.Ok, Is.True); Assert.That(cap.Result.TotalFound, Is.EqualTo(2)); Assert.That(cap.Result.Results.Single().Name, Is.EqualTo("Near"));
            var zero = ScenePerceptionCommands.Spatial(maxResults:0); Assert.That(zero.Result.Results, Is.Empty); Assert.That(zero.Result.TotalFound, Is.EqualTo(2));
        }
        [Test] public void SpatialExactNearReferenceAndComponentFilterNarrowResults()
        {
            var source = Go("Source"); source.transform.position = Vector3.one * 100; var light = Go("Light"); light.transform.position = source.transform.position; light.AddComponent<Light>();
            var r = ScenePerceptionCommands.Spatial(radius:0, nearObject:ExactObjectReference.ExactId(source), componentFilter:"UnityEngine.Light"); Assert.That(r.Ok, Is.True); Assert.That(r.Result.Results.Single().Name, Is.EqualTo("Light"));
        }
        [TestCase(-1)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] public void SpatialRefusesInvalidRadius(float radius) { Assert.That(ScenePerceptionCommands.Spatial(radius:radius).Error.Code, Is.EqualTo("ANALYSIS_INPUT_INVALID")); }
        [Test] public void SpatialRefusesInvalidTypeAndMissingNearReference() { Assert.That(ScenePerceptionCommands.Spatial(componentFilter:"Absent3388").Error.Code, Is.EqualTo("ANALYSIS_TYPE_INVALID")); Assert.That(ScenePerceptionCommands.Spatial(nearObject:"entity:0").Ok, Is.False); }
        [Test] public void MaterialsCountSlotOccurrencesExcludeInactiveAndCapUsers()
        {
            for (var i = 0; i < 3; i++) Go("Renderer" + i).AddComponent<MeshRenderer>().sharedMaterials = new[] { material, material };
            var inactive = Go("Inactive"); inactive.AddComponent<MeshRenderer>().sharedMaterial = material; inactive.SetActive(false);
            var r = ScenePerceptionCommands.Materials(); Assert.That(r.Ok, Is.True); Assert.That(r.Result.TotalMaterials, Is.EqualTo(1)); var m = r.Result.Shaders.Single().Materials.Single(); Assert.That(m.UserCount, Is.EqualTo(6)); Assert.That(m.Users.Count, Is.EqualTo(5)); Assert.That(m.Properties, Is.Null);
            Assert.That(ScenePerceptionCommands.Materials(true).Result.Shaders.Single().Materials.Single().Properties, Is.Not.Null);
        }
        [Test] public void PerformancePreservesStrictShadowThresholdAndOkCase()
        {
            for (var i = 0; i < 4; i++) Go("Light" + i).AddComponent<Light>().shadows = LightShadows.Hard;
            var ok = ScenePerceptionCommands.Performance(); Assert.That(ok.Ok, Is.True); Assert.That(ok.Result.Hints.Single().Category, Is.EqualTo("OK"));
            Go("Fifth").AddComponent<Light>().shadows = LightShadows.Hard; var r = ScenePerceptionCommands.Performance(); Assert.That(r.Result.Hints.Single().Category, Is.EqualTo("Lighting")); Assert.That(r.Result.Hints.Single().Priority, Is.EqualTo(1)); Assert.That(r.Result.Note, Does.Contain("heuristic"));
        }
        [Test] public void ReportsPreserveSceneDirtySelectionAndSerializedState()
        {
            var go = Go("Selected"); Selection.activeGameObject = go; EditorSceneManager.SaveScene(first); var before = EditorJsonUtility.ToJson(go); var r = ScenePerceptionCommands.Summary(); Assert.That(r.Ok, Is.True);
            Assert.That(ScenePerceptionCommands.Health().Ok, Is.True); Assert.That(ScenePerceptionCommands.Materials().Ok, Is.True); Assert.That(first.isDirty, Is.False); Assert.That(Selection.activeGameObject, Is.SameAs(go)); Assert.That(EditorJsonUtility.ToJson(go), Is.EqualTo(before));
        }
        [Test] public void NumericBoundsRefuseBeforeCollection() { Assert.That(ScenePerceptionCommands.Summary(topComponentsLimit:-1).Error.Code, Is.EqualTo("ANALYSIS_INPUT_INVALID")); Assert.That(ScenePerceptionCommands.Hotspots(-1).Error.Code, Is.EqualTo("ANALYSIS_INPUT_INVALID")); Assert.That(ScenePerceptionCommands.Health(-1).Error.Code, Is.EqualTo("ANALYSIS_INPUT_INVALID")); }
    }
}
