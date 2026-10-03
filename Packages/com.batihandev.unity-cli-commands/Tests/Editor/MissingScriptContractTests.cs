using System;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityObject=UnityEngine.Object;
using UnityScene=UnityEngine.SceneManagement.Scene;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class MissingScriptContractTests
    {
        private UnityScene scene;private GameObject root;private string folder;
        [SetUp] public void Setup(){scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);root=new GameObject("RealMissing");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);folder="Assets/MissingAnalysis_"+Guid.NewGuid().ToString("N");System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();Undo.ClearAll();}
        [TearDown] public void Cleanup(){Undo.ClearAll();if(scene.IsValid())EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);AssetDatabase.DeleteAsset(folder);}
        private void MakeMissing()
        {
            var component=root.AddComponent<AnalysisReferenceFixture>();using(var serialized=new SerializedObject(component)){serialized.FindProperty("m_Script").objectReferenceValue=null;serialized.ApplyModifiedPropertiesWithoutUndo();}
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root),Is.EqualTo(1),"The public fixture must contain a real unresolved MonoBehaviour before exercising the command.");Undo.ClearAll();
        }
        [Test] public void ActualMissingScriptReportAndPreviewPreserveUnresolvedComponent()
        {
            MakeMissing();root.SetActive(false);var scan=SceneAnalysisCommands.Missing();Assert.That(scan.Ok,Is.True);Assert.That(scan.Result.MissingScripts,Is.GreaterThanOrEqualTo(1));Assert.That(SceneAnalysisCommands.Missing(false).Ok,Is.True);
            var preview=MissingScriptCommands.Fix(confirm:true,dryRun:true);Assert.That(preview.Ok,Is.True);Assert.That(preview.Result.RemovedComponents,Is.Zero);Assert.That(preview.Result.SelectedCount,Is.GreaterThanOrEqualTo(1));Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root),Is.EqualTo(1));
        }
        [Test] public void ActualMissingRemovalUndoRedoRestoresUnresolvedSlot()
        {
            MakeMissing();var result=MissingScriptCommands.Fix(confirm:true,dryRun:false);Assert.That(result.Ok,Is.True);Assert.That(result.Result.RemovedComponents,Is.GreaterThanOrEqualTo(1));Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root),Is.Zero);
            Undo.FlushUndoRecordObjects();Undo.PerformUndo();Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root),Is.EqualTo(1));Undo.PerformRedo();Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root),Is.Zero);
        }
        [Test] public void MissingRemovalRequiresConfirmationAndDryRunDefaultPreservesState()
        {
            MakeMissing();var refused=MissingScriptCommands.Fix(dryRun:false);Assert.That(refused.Ok,Is.False);Assert.That(refused.Error.Code,Is.EqualTo("CONFIRMATION_REQUIRED"));Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root),Is.EqualTo(1));var preview=MissingScriptCommands.Fix();Assert.That(preview.Ok,Is.True);Assert.That(preview.Result.Applied,Is.False);
        }
        [Test] public void MissingRemovalPersistsThroughSaveAndReopen()
        {
            MakeMissing();var result=MissingScriptCommands.Fix(confirm:true,dryRun:false);Assert.That(result.Ok,Is.True);var path=folder+"/clean.unity";Assert.That(EditorSceneManager.SaveScene(scene,path),Is.True);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);root=scene.GetRootGameObjects()[0];Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root),Is.Zero);
        }
        [Test] public void PrefabMissingScanIncludesRegisteredPackageAssetsAndUnloadsContents()
        {
            var path="Packages/com.batihandev.unity-cli-commands/Tests/Fixtures/MissingScript.prefab";var before=System.IO.File.ReadAllBytes(path);var loadedScenes=UnityEngine.SceneManagement.SceneManager.sceneCount;
            var contents=PrefabUtility.LoadPrefabContents(path);try{Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(contents),Is.EqualTo(1));}finally{PrefabUtility.UnloadPrefabContents(contents);}
            var report=SceneAnalysisCommands.Missing(includeReferences:false,searchInPrefabs:true);Assert.That(report.Ok,Is.True);Assert.That(report.Result.Issues.Exists(issue=>issue.PrefabPath==path && issue.Kind=="MissingScript"),Is.True);Assert.That(System.IO.File.ReadAllBytes(path),Is.EqualTo(before));Assert.That(UnityEngine.SceneManagement.SceneManager.sceneCount,Is.EqualTo(loadedScenes));
        }
        [Test] public void PrefabMissingScanIncludesInactiveChildWhenSceneInactiveObjectsAreExcluded()
        {
            const string source="Packages/com.batihandev.unity-cli-commands/Tests/Fixtures/InactiveMissingScript.prefab";
            var sourceBefore=System.IO.File.ReadAllBytes(source);var path=folder+"/inactiveMissing.prefab";Assert.That(AssetDatabase.CopyAsset(source,path),Is.True);var before=System.IO.File.ReadAllBytes(path);var loadedScenes=UnityEngine.SceneManagement.SceneManager.sceneCount;
            var contents=PrefabUtility.LoadPrefabContents(path);string childName;
            try {var missing=System.Linq.Enumerable.Single(contents.GetComponentsInChildren<UnityEngine.Transform>(true),x=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)==1);Assert.That(missing.parent,Is.Not.Null);Assert.That(missing.gameObject.activeSelf,Is.False);childName=missing.name;}
            finally {PrefabUtility.UnloadPrefabContents(contents);}
            MakeMissing();root.SetActive(false);
            var report=SceneAnalysisCommands.Missing(includeInactive:false,includeReferences:false,searchInPrefabs:true);Assert.That(report.Ok,Is.True);Assert.That(report.Result.Issues.Exists(x=>x.PrefabPath==path&&x.Path.EndsWith("/"+childName)&&x.Kind=="MissingScript"),Is.True);Assert.That(report.Result.Issues.Exists(x=>x.PrefabPath==null&&x.Path==root.name&&x.Kind=="MissingScript"),Is.False);
            Assert.That(System.IO.File.ReadAllBytes(path),Is.EqualTo(before));Assert.That(System.IO.File.ReadAllBytes(source),Is.EqualTo(sourceBefore));Assert.That(UnityEngine.SceneManagement.SceneManager.sceneCount,Is.EqualTo(loadedScenes));
        }
        [Test] public void PrefabMissingScanIncludesLoadedHiddenDescendantWhileSceneHiddenObjectsAreExcluded()
        {
            const string source="Packages/com.batihandev.unity-cli-commands/Tests/Fixtures/HiddenMissingScript.prefab";
            var sourceBefore=System.IO.File.ReadAllBytes(source);var path=folder+"/hiddenMissing.prefab";Assert.That(AssetDatabase.CopyAsset(source,path),Is.True);
            var before=System.IO.File.ReadAllBytes(path);var loadedScenes=UnityEngine.SceneManagement.SceneManager.sceneCount;
            var contents=PrefabUtility.LoadPrefabContents(path);string childName;
            try
            {
                Assert.That(contents.GetComponent<PrefabHiddenChildFixture>(),Is.Not.Null);
                var missing=System.Linq.Enumerable.Single(contents.GetComponentsInChildren<UnityEngine.Transform>(true),x=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject)==1);
                Assert.That(missing.parent,Is.Not.Null);childName=missing.name;
                using(var serialized=new SerializedObject(missing.gameObject))Assert.That(serialized.FindProperty("m_ObjectHideFlags").intValue,Is.EqualTo((int)HideFlags.HideInHierarchy),"The native-loaded prefab must contain a real hidden missing-script descendant before exercising analysis.");
            }
            finally {PrefabUtility.UnloadPrefabContents(contents);}
            MakeMissing();root.hideFlags=HideFlags.HideInHierarchy;
            var report=SceneAnalysisCommands.Missing(includeReferences:false,searchInPrefabs:true);
            Assert.That(report.Ok,Is.True,"The read-only scene and prefab scan must succeed.");
            Assert.That(report.Result.Issues.Exists(x=>x.PrefabPath==path&&x.Path.EndsWith("/"+childName)&&x.Kind=="MissingScript"),Is.True,"Prefab scope must include the genuinely missing descendant hidden by its authored fixture component.");
            Assert.That(report.Result.Issues.Exists(x=>x.PrefabPath==null&&x.Path==root.name&&x.Kind=="MissingScript"),Is.False,"Scene scope must continue excluding hidden objects.");
            Assert.That(System.IO.File.ReadAllBytes(path),Is.EqualTo(before));Assert.That(System.IO.File.ReadAllBytes(source),Is.EqualTo(sourceBefore));Assert.That(UnityEngine.SceneManagement.SceneManager.sceneCount,Is.EqualTo(loadedScenes));
        }
        [Test] public void SceneFixDoesNotModifyPrefabMissingScriptsAndReadOnlyContentsAlwaysUnload()
        {
            var path=folder+"/missing.prefab";Assert.That(AssetDatabase.CopyAsset("Packages/com.batihandev.unity-cli-commands/Tests/Fixtures/MissingScript.prefab",path),Is.True);MakeMissing();var before=System.IO.File.ReadAllBytes(path);var contents=PrefabUtility.LoadPrefabContents(path);
            try{Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(contents),Is.EqualTo(1));var scan=SceneAnalysisCommands.Missing();Assert.That(scan.Ok,Is.True);Assert.That(scan.Result.Issues.Exists(x=>x.Target==BatihanDev.UnityCliCommands.Editor.ExactObjectReference.ExactId(contents)),Is.False);}finally{PrefabUtility.UnloadPrefabContents(contents);}
            var result=MissingScriptCommands.Fix(confirm:true,dryRun:false);Assert.That(result.Ok,Is.True);Assert.That(System.IO.File.ReadAllBytes(path),Is.EqualTo(before));
        }
    }
}
