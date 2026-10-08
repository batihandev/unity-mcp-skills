using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject=UnityEngine.Object;
using UnityScene=UnityEngine.SceneManagement.Scene;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class SceneAnalysisContractTests
    {
        private UnityScene scene;private UnityEngine.Material opaque,a,b;private Mesh mesh;
        [SetUp] public void Setup(){scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);opaque=new UnityEngine.Material(Shader.Find("Hidden/InternalErrorShader")){renderQueue=2000};a=new UnityEngine.Material(opaque){renderQueue=2500};b=new UnityEngine.Material(opaque){renderQueue=3000};mesh=new Mesh{vertices=new[]{Vector3.zero,Vector3.right,Vector3.up},triangles=new[]{0,1,2}};}
        [TearDown] public void Cleanup(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);UnityObject.DestroyImmediate(opaque);UnityObject.DestroyImmediate(a);UnityObject.DestroyImmediate(b);UnityObject.DestroyImmediate(mesh);}
        private GameObject Renderer(string name,bool active=true){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=new[]{opaque,null,a,b};go.SetActive(active);return go;}
        [Test] public void RenderingIncludesDisabledComponentsExcludesInactiveAndUsesStrictThresholds()
        {
            var go=Renderer("Disabled");go.GetComponent<MeshRenderer>().enabled=false;Renderer("Inactive",false);
            var r=SceneAnalysisCommands.Rendering(1,4);Assert.That(r.Ok,Is.True);Assert.That(r.Result.TotalRenderers,Is.GreaterThanOrEqualTo(1));Assert.That(r.Result.Issues.Where(x=>x.Path=="Disabled"),Is.Empty);
            var flagged=SceneAnalysisCommands.Rendering(0,3);Assert.That(flagged.Ok,Is.True);Assert.That(flagged.Result.Issues.Count(x=>x.Path=="Disabled"),Is.EqualTo(2));Assert.That(flagged.Result.Issues.Any(x=>x.Path=="Inactive"),Is.False);
        }
        [Test] public void TransparentSelectsFirstQualifyingSlotInclusiveQueueAndKeepsDisabledRenderer()
        {
            var go=Renderer("Candidate");go.GetComponent<MeshRenderer>().enabled=false;Renderer("Inactive",false);var r=SceneAnalysisCommands.Transparent();Assert.That(r.Ok,Is.True);var row=r.Result.Objects.Single(x=>x.Path=="Candidate");Assert.That(row.RenderQueue,Is.EqualTo(2500));Assert.That(row.Material,Is.EqualTo(BatihanDev.UnityCliCommands.Editor.ExactObjectReference.ExactId(a)));Assert.That(r.Result.Objects.Any(x=>x.Path=="Inactive"),Is.False);Assert.That(r.Result.Note,Does.Contain("heuristic"));
        }
        [Test] public void TransparentCapShowsTotalBeforeTruncation(){Renderer("One");Renderer("Two");var r=SceneAnalysisCommands.Transparent(1);Assert.That(r.Ok,Is.True);Assert.That(r.Result.Total,Is.GreaterThanOrEqualTo(2));Assert.That(r.Result.Objects.Count,Is.EqualTo(1));Assert.That(r.Result.Truncated,Is.True);}
        [Test] public void MissingIgnoresLegitimateNullAndHonorsInactivePolicy()
        {
            var go=new GameObject("InactiveNull");SceneManager.MoveGameObjectToScene(go,scene);go.AddComponent<AnalysisReferenceFixture>();go.SetActive(false);var r=SceneAnalysisCommands.Missing();Assert.That(r.Ok,Is.True);Assert.That(r.Result.Issues.Where(x=>x.Path=="InactiveNull"),Is.Empty);Assert.That(SceneAnalysisCommands.Missing(false).Ok,Is.True);
        }
        [TestCase(-1,5)] [TestCase(10000,-1)] public void RenderingRejectsNegativeThresholds(int poly,int slots){Assert.That(SceneAnalysisCommands.Rendering(poly,slots).Error.Code,Is.EqualTo("ANALYSIS_INPUT_INVALID"));}
        [Test] public void TransparentRejectsNegativeCap(){Assert.That(SceneAnalysisCommands.Transparent(-1).Error.Code,Is.EqualTo("ANALYSIS_INPUT_INVALID"));}
    }
}
