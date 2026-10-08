using System.Linq;
using BatihanDev.UnityCliCommands.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public abstract class SmartFixtureSupport
    {
        protected const string Folder = "Assets/__Smart3393";
        protected UnityEngine.SceneManagement.Scene First;
        private UnityObject[] selection = System.Array.Empty<UnityObject>();
        private UnityEngine.Random.State random;
        private bool ownsFolder;
        [SetUp] public void SetUpSmart()
        {
            selection = Selection.objects; random = UnityEngine.Random.state;
            First = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.That(AssetDatabase.IsValidFolder(Folder), Is.False, "Refuse an existing unowned fixture folder.");
            AssetDatabase.CreateFolder("Assets", "__Smart3393"); ownsFolder = true;
            Assert.That(EditorSceneManager.SaveScene(First, Folder + "/Base.unity"), Is.True);
        }
        [TearDown] public void TearDownSmart()
        {
            Selection.objects = selection.Where(x => x != null).ToArray(); UnityEngine.Random.state = random;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (ownsFolder) AssetDatabase.DeleteAsset(Folder);
        }
        protected GameObject Go(string name, Vector3 position = default, GameObject parent = null)
        {
            var value = new GameObject(name); SceneManager.MoveGameObjectToScene(value, First);
            if (parent != null) value.transform.SetParent(parent.transform);
            value.transform.position = position; return value;
        }
        protected string Targets(params GameObject[] objects) => "[" + string.Join(",", objects.Select(x => "\"" + ExactObjectReference.ExactId(x) + "\"")) + "]";
        protected string Id(UnityObject value) => ExactObjectReference.ExactId(value);
        protected void Near(Vector3 value, Vector3 expected) => Assert.That(Vector3.Distance(value, expected), Is.LessThan(.0002f));
        protected void SameRotation(Quaternion value, Quaternion expected) => Assert.That(Quaternion.Angle(value, expected), Is.LessThan(.02f));
        protected string Prefab(string name = "Replacement")
        {
            var go = Go(name); var path = Folder + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path); UnityObject.DestroyImmediate(go); return path;
        }
    }
}
