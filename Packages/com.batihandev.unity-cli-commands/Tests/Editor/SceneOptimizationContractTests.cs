using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityObject=UnityEngine.Object;
using UnityScene=UnityEngine.SceneManagement.Scene;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class SceneOptimizationContractTests
    {
        private UnityScene scene;private GameObject root,child;private string id;
        [SetUp] public void Setup(){scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);root=new GameObject("OptimizationRoot");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);child=GameObject.CreatePrimitive(PrimitiveType.Cube);child.transform.SetParent(root.transform);id=BatihanDev.UnityCliCommands.Editor.ExactObjectReference.ExactId(root);Undo.ClearAll();}
        [TearDown] public void Cleanup(){Undo.ClearAll();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        [Test] public void StaticDefaultsRootOnlyAndUndoRestoresFlags(){var r=SceneOptimizationCommands.StaticSet(id,confirm:true);Assert.That(r.Ok,Is.True);Assert.That(r.Result.AppliedCount,Is.EqualTo(1));Assert.That(root.isStatic,Is.True);Assert.That(child.isStatic,Is.False);Undo.PerformUndo();Assert.That(root.isStatic,Is.False);Undo.PerformRedo();Assert.That(root.isStatic,Is.True);}
        [Test] public void StaticSubtreeDeduplicatesRootIncludesInactiveAndDryRunWins(){child.SetActive(false);var preview=SceneOptimizationCommands.StaticSet(id,"BatchingStatic,OccluderStatic",true,true,true);Assert.That(preview.Ok,Is.True);Assert.That(preview.Result.SelectedCount,Is.EqualTo(2));Assert.That(preview.Result.AppliedCount,Is.Zero);Assert.That(root.isStatic,Is.False);var r=SceneOptimizationCommands.StaticSet(id,"BatchingStatic,OccluderStatic",true,true);Assert.That(r.Ok,Is.True);Assert.That(r.Result.AppliedCount,Is.EqualTo(2));Assert.That(GameObjectUtility.GetStaticEditorFlags(child),Is.EqualTo(StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic));Undo.PerformUndo();Assert.That(child.isStatic,Is.False);}
        [TestCase("Bogus")] [TestCase("12345")] [TestCase("BatchingStatic,Bogus")] public void InvalidNamedFlagsRefuseWholeSubtree(string flags){var r=SceneOptimizationCommands.StaticSet(id,flags,true,true);Assert.That(r.Ok,Is.False);Assert.That(r.Error.Code,Is.EqualTo("STATIC_FLAGS_INVALID"));Assert.That(root.isStatic,Is.False);Assert.That(child.isStatic,Is.False);}
        [Test] public void BothMutatorsRequireConfirmation(){Assert.That(SceneOptimizationCommands.StaticSet(id).Error.Code,Is.EqualTo("CONFIRMATION_REQUIRED"));Assert.That(SceneOptimizationCommands.LodSetup(id).Error.Code,Is.EqualTo("CONFIRMATION_REQUIRED"));Assert.That(root.GetComponent<LODGroup>(),Is.Null);}
        [Test] public void LodDefaultsFirstLevelActiveRenderersLaterEmptyAndUndoRemovesCreatedComponent(){var hidden=new GameObject("Hidden");hidden.transform.SetParent(root.transform);hidden.AddComponent<MeshRenderer>();hidden.SetActive(false);var r=SceneOptimizationCommands.LodSetup(id,confirm:true);Assert.That(r.Ok,Is.True);var group=root.GetComponent<LODGroup>();var lods=group.GetLODs();Assert.That(lods.Select(x=>x.screenRelativeTransitionHeight),Is.EqualTo(new[]{.6f,.3f,.1f,0f}));Assert.That(lods[0].renderers,Is.EqualTo(new[]{child.GetComponent<Renderer>()}));Assert.That(lods.Skip(1).All(x=>x.renderers.Length==0),Is.True);Assert.That(group.size,Is.GreaterThan(0));Undo.PerformUndo();Assert.That(root.GetComponent<LODGroup>(),Is.Null);Undo.PerformRedo();Assert.That(root.GetComponent<LODGroup>().GetLODs().Length,Is.EqualTo(4));}
        [Test] public void LodDryRunDoesNotAddComponent(){var r=SceneOptimizationCommands.LodSetup(id,confirm:true,dryRun:true);Assert.That(r.Ok,Is.True);Assert.That(r.Result.Applied,Is.False);Assert.That(root.GetComponent<LODGroup>(),Is.Null);}
        [TestCase("")] [TestCase("NaN,.3")] [TestCase("Infinity,.3")] [TestCase(".2,.6")] [TestCase(".5,.5")] [TestCase("1.1,.3")] [TestCase(".6,0")] [TestCase("-.1")]
        public void InvalidLodThresholdsRefuseBeforeAdd(string distances){var r=SceneOptimizationCommands.LodSetup(id,distances,true);Assert.That(r.Ok,Is.False);Assert.That(r.Error.Code,Is.EqualTo("LOD_DISTANCES_INVALID"));Assert.That(root.GetComponent<LODGroup>(),Is.Null);}
        [Test] public void ExistingLodFullStateUndoRestoresPreviousConfiguration(){var group=root.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.75f,new[]{child.GetComponent<Renderer>()})});group.size=12;group.localReferencePoint=new Vector3(1,2,3);var r=SceneOptimizationCommands.LodSetup(id,".7,.2",true);Assert.That(r.Ok,Is.True);Undo.PerformUndo();Assert.That(group.GetLODs().Length,Is.EqualTo(1));Assert.That(group.GetLODs()[0].screenRelativeTransitionHeight,Is.EqualTo(.75f));Assert.That(group.size,Is.EqualTo(12));Assert.That(group.localReferencePoint,Is.EqualTo(new Vector3(1,2,3)));}
    }
}
