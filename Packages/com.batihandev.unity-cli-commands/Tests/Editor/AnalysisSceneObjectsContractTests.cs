using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject=UnityEngine.Object;
using UnityScene=UnityEngine.SceneManagement.Scene;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class AnalysisSceneObjectsContractTests
    {
        private UnityScene first,second;private string folder;
        [SetUp] public void Setup(){first=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);folder="Assets/SceneObjectsAnalysis_"+Guid.NewGuid().ToString("N");System.IO.Directory.CreateDirectory(folder);UnityEditor.AssetDatabase.Refresh();Assert.That(EditorSceneManager.SaveScene(first,folder+"/first.unity"),Is.True);second=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);}
        [TearDown] public void Cleanup(){if(second.IsValid())EditorSceneManager.CloseScene(second,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);if(!string.IsNullOrEmpty(folder))UnityEditor.AssetDatabase.DeleteAsset(folder);}
        [Test] public void LoadedIncludesTwoScenesAndInactiveHierarchyWithoutCaching()
        {
            var a=new GameObject("First");SceneManager.MoveGameObjectToScene(a,first);var b=new GameObject("Second");SceneManager.MoveGameObjectToScene(b,second);var child=new GameObject("Inactive");child.transform.SetParent(a.transform);child.SetActive(false);
            Assert.That(AnalysisSceneObjects.Loaded().Where(x=>x.scene==first||x.scene==second),Is.EquivalentTo(new[]{a,b,child}));Assert.That(AnalysisSceneObjects.Loaded(false),Has.No.Member(child));child.SetActive(true);Assert.That(AnalysisSceneObjects.Loaded(false),Has.Member(child));Assert.That(AnalysisSceneObjects.Path(child),Is.EqualTo("First/Inactive"));
        }
        [Test] public void VisibleOnlyExcludesHiddenAndAlwaysExcludesPreviewObjects()
        {
            var hidden=new GameObject("Hidden");SceneManager.MoveGameObjectToScene(hidden,first);hidden.hideFlags=HideFlags.HideInHierarchy;Assert.That(AnalysisSceneObjects.Loaded(),Has.Member(hidden));Assert.That(AnalysisSceneObjects.Loaded(visibleOnly:true),Has.No.Member(hidden));
            var preview=EditorSceneManager.NewPreviewScene();try {var go=new GameObject("Preview");SceneManager.MoveGameObjectToScene(go,preview);Assert.That(AnalysisSceneObjects.Loaded(),Has.No.Member(go));}finally{EditorSceneManager.ClosePreviewScene(preview);}
        }
        [Test] public void QualifiedAndUniqueCaseInsensitiveTypesResolveAndLoadedTypesIncludesPlainClasses()
        {
            Assert.That(AnalysisSceneObjects.ResolveType("unityengine.meshRenderer",typeof(UnityEngine.Component)),Is.EqualTo(typeof(MeshRenderer)));Assert.That(AnalysisSceneObjects.ResolveType("meshrenderer",typeof(UnityEngine.Component)),Is.EqualTo(typeof(MeshRenderer)));Assert.That(AnalysisSceneObjects.LoadedTypes(),Has.Member(typeof(AnalysisSceneObjectsContractTests)));
        }
        [TestCase("DefinitelyAbsentAnalysisType")] [TestCase("AnalysisTypeCollision")] [TestCase("System.String")]
        public void MissingAmbiguousOrIncompatibleTypesRefuse(string name){Assert.Throws<ArgumentException>(()=>AnalysisSceneObjects.ResolveType(name,typeof(UnityEngine.Component)));}
    }
}
