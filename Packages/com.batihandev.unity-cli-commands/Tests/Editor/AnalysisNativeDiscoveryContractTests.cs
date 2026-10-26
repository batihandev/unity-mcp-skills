using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class AnalysisNativeDiscoveryContractTests : SmartFixtureSupport
    {
        private static readonly int[] Flags = { 0, 1, 2, 4, 8, 16, 32, 52 };
        private static System.Collections.Generic.IEnumerable<TestCaseData> FlagCases()
        {
            foreach (var flag in Flags) foreach (var componentLocal in new[] { false, true })
                yield return new TestCaseData(flag, componentLocal);
        }
        private static UnityEngine.Component[] Native(Type type) => UnityObject.FindObjectsOfType(type).OfType<UnityEngine.Component>().Where(value => value.gameObject.scene.IsValid() && value.gameObject.scene.isLoaded && !EditorSceneManager.IsPreviewScene(value.gameObject.scene) && value.gameObject.activeInHierarchy).ToArray();
        private static void Flag(GameObject go, UnityEngine.Component component, int flags, bool componentLocal)
        { if (componentLocal) component.hideFlags = (HideFlags)flags; else go.hideFlags = (HideFlags)flags; }
        [TestCaseSource(nameof(FlagCases))]
        public void QueryPreservesNativeEligibilityOrderOccurrencesTotalsCapsAndDiagnostics(int flags, bool componentLocal)
        {
            var owned = Go("Flagged"); var first = owned.AddComponent<SmartQueryFixture>(); first.Number = 9;
            var second = owned.AddComponent<SmartQueryFixture>(); second.Number = 9;
            Flag(owned, first, flags, componentLocal); if (componentLocal) second.hideFlags = (HideFlags)flags;
            var additive = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var other = new GameObject("Additive"); SceneManager.MoveGameObjectToScene(other, additive); other.AddComponent<SmartQueryFixture>().Number = 9;
            Go("Inactive").AddComponent<SmartQueryFixture>().gameObject.SetActive(false);
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var previewGo = new GameObject("Preview"); SceneManager.MoveGameObjectToScene(previewGo, preview); previewGo.AddComponent<SmartQueryFixture>().Number = 9;
                var expected = Native(typeof(SmartQueryFixture)).Cast<SmartQueryFixture>().Where(value => value.Number == 9).Select(value => Id(value)).ToArray();
                Assert.That(expected, Does.Contain(Id(other.GetComponent<SmartQueryFixture>())), "Additive native fixture prerequisite");
                var full = SmartQueryCommands.Query(typeof(SmartQueryFixture).FullName, "Number", "==", "9");
                Assert.That(full.Ok, Is.True); Assert.That(full.Result.Total, Is.EqualTo(expected.Length)); Assert.That(full.Result.Items.Select(value => value.Component), Is.EqualTo(expected));
                var capped = SmartQueryCommands.Query(typeof(SmartQueryFixture).FullName, "Number", "==", "9", 1);
                Assert.That(capped.Ok, Is.True); Assert.That(capped.Result.Total, Is.EqualTo(expected.Length)); Assert.That(capped.Result.Items.Select(value => value.Component), Is.EqualTo(expected.Take(1))); Assert.That(capped.Result.Truncated, Is.EqualTo(expected.Length > 1));
                var failures = SmartQueryCommands.Query(typeof(SmartQueryFixture).FullName, "Throwing", ">", "0", 0);
                Assert.That(failures.Ok, Is.True); Assert.That(failures.Result.Diagnostics.Select(value => value.Component), Is.EqualTo(Native(typeof(SmartQueryFixture)).Select(value => Id(value)))); Assert.That(failures.Result.Diagnostics.All(value => value.Code == "GETTER_FAILED"), Is.True);
            }
            finally { owned.hideFlags = HideFlags.None; first.hideFlags = HideFlags.None; second.hideFlags = HideFlags.None; EditorSceneManager.ClosePreviewScene(preview); }
        }
        [TestCaseSource(nameof(FlagCases))]
        public void RenderingTransparentMaterialsAndMeshCollidersPreserveNativeFlags(int flags, bool componentLocal)
        {
            var go = Go("NativeFlags"); var renderer = go.AddComponent<MeshRenderer>(); var collider = go.AddComponent<MeshCollider>();
            var shader = Shader.Find("Hidden/InternalErrorShader"); Assert.That(shader, Is.Not.Null, "Native shader fixture prerequisite");
            var material = new Material(shader) { renderQueue = 3000 }; renderer.sharedMaterial = material;
            Flag(go, renderer, flags, componentLocal); if (componentLocal) collider.hideFlags = (HideFlags)flags;
            try
            {
                var renderers = Native(typeof(Renderer)); var colliders = Native(typeof(MeshCollider));
                var rendering = SceneAnalysisCommands.Rendering(); Assert.That(rendering.Ok, Is.True); Assert.That(rendering.Result.TotalRenderers, Is.EqualTo(renderers.Length));
                var transparent = SceneAnalysisCommands.Transparent(0); Assert.That(transparent.Ok, Is.True); Assert.That(transparent.Result.Total, Is.EqualTo(renderers.Count(value => ((Renderer)value).sharedMaterials.Any(item => item != null && item.renderQueue >= 2500)))); Assert.That(transparent.Result.Objects, Is.Empty); Assert.That(transparent.Result.Truncated, Is.EqualTo(transparent.Result.Total > 0));
                var materials = ScenePerceptionCommands.Materials(); Assert.That(materials.Ok, Is.True); Assert.That(materials.Result.TotalMaterials, Is.EqualTo(renderers.Cast<Renderer>().SelectMany(value => value.sharedMaterials).Where(value => value != null).Distinct().Count()));
                var result = ValidationDiagnosticsCommands.MeshColliders(0); Assert.That(result.Ok, Is.True); Assert.That(result.Result.Total, Is.EqualTo(colliders.Cast<MeshCollider>().Count(value => !value.convex))); Assert.That(result.Result.Colliders, Is.Empty); Assert.That(result.Result.Truncated, Is.EqualTo(result.Result.Total > 0));
            }
            finally { go.hideFlags = HideFlags.None; renderer.hideFlags = HideFlags.None; collider.hideFlags = HideFlags.None; UnityObject.DestroyImmediate(material); }
        }
        [TestCaseSource(nameof(FlagCases))]
        public void PerformanceAllFourNativeComponentFamiliesPreserveFlags(int flags, bool componentLocal)
        {
            var lights = Enumerable.Range(0, 5).Select(index => { var go = Go("Shadow" + index); var light = go.AddComponent<Light>(); light.shadows = LightShadows.Hard; Flag(go, light, flags, componentLocal); return light; }).ToArray();
            var shader = Shader.Find("Hidden/InternalErrorShader"); Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            var mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up } };
            mesh.SetIndices(Enumerable.Range(0, 30003).Select(index => index % 3).ToArray(), MeshTopology.Triangles, 0);
            var renderers = Enumerable.Range(0, 101).Select(index => { var go = Go("Renderer" + index); var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; Flag(go, renderer, flags, componentLocal); return renderer; }).ToArray();
            var filter = renderers[0].gameObject.AddComponent<MeshFilter>(); filter.sharedMesh = mesh; if (componentLocal) filter.hideFlags = (HideFlags)flags;
            var particles = Enumerable.Range(0, 21).Select(index => { var go = Go("Particles" + index); var particle = go.AddComponent<ParticleSystem>(); var main = particle.main; main.playOnAwake = false; particle.Stop(); Flag(go, particle, flags, componentLocal); var renderer = go.GetComponent<ParticleSystemRenderer>(); if (componentLocal) renderer.hideFlags = (HideFlags)flags; return particle; }).ToArray();
            try
            {
                var count = Native(typeof(Light)).Cast<Light>().Count(value => value.shadows != LightShadows.None);
                var report = ScenePerceptionCommands.Performance(); Assert.That(report.Ok, Is.True);
                Assert.That(report.Result.Hints.Any(value => value.Category == "Lighting"), Is.EqualTo(count > 4));
                if (count > 4) Assert.That(report.Result.Hints.Single(value => value.Category == "Lighting").Issue, Does.StartWith(count + " "));
                var nativeRenderers = Native(typeof(Renderer)).Cast<Renderer>().ToArray();
                var nonstatic = nativeRenderers.Count(value => !value.gameObject.isStatic);
                Assert.That(report.Result.Hints.Any(value => value.Category == "Batching"), Is.EqualTo(nonstatic > 100));
                if (nonstatic > 100) Assert.That(report.Result.Hints.Single(value => value.Category == "Batching").Issue, Does.StartWith(nonstatic + " "));
                var geometry = Native(typeof(MeshFilter)).Cast<MeshFilter>().Count(value => value.sharedMesh == mesh && value.GetComponent<LODGroup>() == null);
                Assert.That(report.Result.Hints.Any(value => value.Category == "Geometry"), Is.EqualTo(geometry > 0));
                var slots = nativeRenderers.SelectMany(value => value.sharedMaterials).Where(value => value != null).ToArray(); var duplicate = slots.Length - slots.Distinct().Count();
                Assert.That(report.Result.Hints.Any(value => value.Category == "Materials"), Is.EqualTo(duplicate > 10));
                var particleCount = Native(typeof(ParticleSystem)).Length;
                Assert.That(report.Result.Hints.Any(value => value.Category == "Particles"), Is.EqualTo(particleCount > 20));
            }
            finally
            {
                foreach (var light in lights) { light.hideFlags = HideFlags.None; light.gameObject.hideFlags = HideFlags.None; }
                foreach (var renderer in renderers) { renderer.hideFlags = HideFlags.None; renderer.gameObject.hideFlags = HideFlags.None; }
                foreach (var particle in particles) { particle.hideFlags = HideFlags.None; particle.gameObject.hideFlags = HideFlags.None; particle.GetComponent<ParticleSystemRenderer>().hideFlags = HideFlags.None; }
                filter.hideFlags = HideFlags.None; UnityObject.DestroyImmediate(material); UnityObject.DestroyImmediate(mesh);
            }
        }
        [Test]
        public void LoadedTypesExcludesRealDynamicAssemblyAndKeepsOrdinaryTypes()
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("AnalysisDynamic_" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
            var emitted = assembly.DefineDynamicModule("AnalysisModule").DefineType("AnalysisDynamicType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public).CreateType();
            Assert.That(assembly.IsDynamic, Is.True); Assert.That(assembly.GetTypes(), Has.Member(emitted), "Reflection.Emit fixture prerequisite");
            var types = AnalysisSceneObjects.LoadedTypes(); Assert.That(types, Has.No.Member(emitted)); Assert.That(types, Has.Member(typeof(AnalysisNativeDiscoveryContractTests)));
        }
    }
}
