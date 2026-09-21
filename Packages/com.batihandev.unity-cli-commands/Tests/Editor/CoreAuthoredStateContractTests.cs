using System;
using System.Collections;
using System.IO;
using System.Linq;
using BatihanDev.UnityCliCommands.Asset;
using BatihanDev.UnityCliCommands.Audio;
using BatihanDev.UnityCliCommands.Component;
using BatihanDev.UnityCliCommands.Scene;
using BatihanDev.UnityCliCommands.ScriptableObject;
using BatihanDev.UnityCliCommands.Transform;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TestTools;
using UnityObject = UnityEngine.Object;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class CoreAuthoredStateContractTests
    {
        private const string Root = "Assets/Task10CoreAuthoredStateTests";

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Root))
                AssetDatabase.CreateFolder("Assets", "Task10CoreAuthoredStateTests");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Root);
            AssetDatabase.Refresh();
        }

        [Test]
        public void WorldAndPhysicalRectCommandsPreserveOmittedAxes()
        {
            var parent = new GameObject("Task10Parent");
            var child = new GameObject("Task10Child");
            var canvas = new GameObject("Task10Canvas", typeof(RectTransform), typeof(Canvas));
            var rectObject = new GameObject("Task10Rect", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            try
            {
                parent.transform.position = new Vector3(10f, 0f, 0f);
                child.transform.SetParent(parent.transform, false);
                child.transform.localPosition = new Vector3(1f, 2f, 3f);
                child.transform.eulerAngles = new Vector3(4f, 5f, 6f);

                var world = TransformCommands.World(Exact(child), posX: 20f, rotY: 35f);
                Assert.That(world.Ok, Is.True, world.Error?.Code);
                Assert.That(child.transform.position.x, Is.EqualTo(20f).Within(0.001f));
                Assert.That(child.transform.position.y, Is.EqualTo(2f).Within(0.001f));
                Assert.That(child.transform.position.z, Is.EqualTo(3f).Within(0.001f));
                Assert.That(Mathf.DeltaAngle(child.transform.eulerAngles.y, 35f), Is.EqualTo(0f).Within(0.01f));

                rectObject.transform.SetParent(canvas.transform, false);
                var rect = (RectTransform)rectObject.transform;
                rect.anchorMin = new Vector2(0.1f, 0.2f);
                rect.anchorMax = new Vector2(0.8f, 0.9f);
                var beforeHeight = rect.rect.height;
                var size = RectTransformCommands.Size(Exact(rectObject), width: 321f);
                Assert.That(size.Ok, Is.True, size.Error?.Code);
                Assert.That(rect.rect.width, Is.EqualTo(321f).Within(0.01f));
                Assert.That(rect.rect.height, Is.EqualTo(beforeHeight).Within(0.01f));
            }
            finally
            {
                UnityObject.DestroyImmediate(rectObject);
                UnityObject.DestroyImmediate(canvas);
                UnityObject.DestroyImmediate(child);
                UnityObject.DestroyImmediate(parent);
            }
        }

        [Test]
        public void ComponentRemoveRefusesRequiredComponentAndCopyUsesExactHandle()
        {
            var source = new GameObject("Task10Source", typeof(AudioSource));
            var destination = new GameObject("Task10Destination");
            var ui = new GameObject("Task10Ui", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            try
            {
                source.GetComponent<AudioSource>().volume = 0.37f;
                var copy = ComponentAuthoringCommands.Copy(Exact(source.GetComponent<AudioSource>()), Exact(destination));
                Assert.That(copy.Ok, Is.True, copy.Error?.Code);
                Assert.That(destination.GetComponent<AudioSource>().volume, Is.EqualTo(0.37f).Within(0.001f));

                var required = ui.GetComponent<CanvasRenderer>();
                var refused = ComponentAuthoringCommands.Remove(Exact(required));
                Assert.That(refused.Ok, Is.False);
                Assert.That(refused.Error.Code, Is.EqualTo("REQUIRED_COMPONENT"));
                Assert.That(ui.GetComponent<CanvasRenderer>(), Is.SameAs(required));
                Assert.That(ui.GetComponent<Image>(), Is.Not.Null);
            }
            finally
            {
                UnityObject.DestroyImmediate(ui);
                UnityObject.DestroyImmediate(destination);
                UnityObject.DestroyImmediate(source);
            }
        }

        [Test]
        public void ComponentMemberSetResolvesUniqueCaseInsensitivePublicProperty()
        {
            var gameObject = new GameObject("Task10Collider", typeof(BoxCollider));
            try
            {
                var collider = gameObject.GetComponent<BoxCollider>();
                Assert.That(collider.hasModifiableContacts, Is.False);
                var result = ComponentAuthoringCommands.MemberSet(Exact(collider), "HASMODIFIABLECONTACTS", "true");
                Assert.That(result.Ok, Is.True, result.Error?.Code);
                Assert.That(result.Result.Property, Is.EqualTo("hasModifiableContacts"));
                Assert.That(collider.hasModifiableContacts, Is.True);
            }
            finally
            {
                UnityObject.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AssetCommandsRequireAuthorizationAndReadBackLabelsAndReimport()
        {
            var path = Root + "/Labels.asset";
            AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")), path);
            AssetDatabase.SaveAssets();

            var refused = AssetAuthoringCommands.Labels(path, new[] { "one", "two" });
            Assert.That(refused.Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));
            Assert.That(AssetDatabase.GetLabels(AssetDatabase.LoadMainAssetAtPath(path)), Is.Empty);

            var applied = AssetAuthoringCommands.Labels(path, new[] { "one", "two" }, confirm: true);
            Assert.That(applied.Ok, Is.True, applied.Error?.Code);
            Assert.That(applied.Result.After, Is.EquivalentTo(new[] { "one", "two" }));

            var reimport = AssetAuthoringCommands.Reimport(path, dryRun: true);
            Assert.That(reimport.Ok, Is.True, reimport.Error?.Code);
            Assert.That(reimport.Result.Applied, Is.False);
        }

        [Test]
        public void AssetTrashRefusesWithoutConfirmationAndRemovesOnlyOwnedAsset()
        {
            var path = Root + "/Trash.asset";
            AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")), path);
            AssetDatabase.SaveAssets();
            var refused = AssetAuthoringCommands.Trash(path);
            Assert.That(refused.Error.Code, Is.EqualTo("CONFIRMATION_REQUIRED"));
            Assert.That(AssetDatabase.LoadMainAssetAtPath(path), Is.Not.Null);
            var removed = AssetAuthoringCommands.Trash(path, confirm: true);
            Assert.That(removed.Ok, Is.True, removed.Error?.Code);
            Assert.That(AssetDatabase.LoadMainAssetAtPath(path), Is.Null);
        }

        [Test]
        public void AudioMixerFactoryCreatesOneLoadableMasterGroup()
        {
            var result = AudioAuthoringCommands.CreateMixer("Task10Mixer", Root, confirm: true);
            Assert.That(result.Ok, Is.True, result.Error?.Code);
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(result.Result.Path);
            Assert.That(mixer, Is.Not.Null);
            Assert.That(mixer.FindMatchingGroups("Master").Count(group => group.name == "Master"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ScriptableObjectMemberAndJsonImportPreserveOmittedValues()
        {
            var path = Root + "/Settings.asset";
            var referencePath = Root + "/Reference.mat";
            var asset = UnityEngine.ScriptableObject.CreateInstance<CoreAuthoredStateFixtureAsset>();
            var reference = new Material(Shader.Find("Standard"));
            asset.Number = 3;
            asset.Text = "before";
            asset.Reference = reference;
            AssetDatabase.CreateAsset(reference, referencePath);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            asset = AssetDatabase.LoadAssetAtPath<CoreAuthoredStateFixtureAsset>(path);
            yield return null;

            asset.Number = 4;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            yield return null;
            var serializedPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
            StringAssert.Contains("Number: 4", File.ReadAllText(serializedPath), "Direct control save did not persist.");
            asset.Number = 3;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            yield return null;

            var flat = ScriptableObjectAuthoringCommands.ImportJson(path, "{\"Number\":9}", confirm: true);
            Assert.That(flat.Ok, Is.False);
            Assert.That(flat.Error.Code, Is.EqualTo("JSON_ROOT_INVALID"));
            Assert.That(asset.Number, Is.EqualTo(3), "A flat runtime JSON payload must not mutate the asset.");
            foreach (var invalidRoot in new[] { "{}", "{\"MonoBehaviour\":null}", "{\"MonoBehaviour\":9}", "{\"WrongRoot\":{}}", "{\"MonoBehaviour\":{\"Number\":9},\"MonoBehaviour\":{\"Number\":10}}" })
            {
                var refused = ScriptableObjectAuthoringCommands.ImportJson(path, invalidRoot, confirm: true);
                Assert.That(refused.Ok, Is.False, invalidRoot);
                Assert.That(refused.Error.Code, Is.EqualTo("JSON_ROOT_INVALID"), invalidRoot);
                Assert.That(asset.Number, Is.EqualTo(3), invalidRoot);
            }

            var exported = EditorJsonUtility.ToJson(asset);
            StringAssert.Contains("\"MonoBehaviour\"", exported);
            StringAssert.Contains("\"Number\":3", exported);
            var roundTrip = ScriptableObjectAuthoringCommands.ImportJson(path, exported, dryRun: true);
            Assert.That(roundTrip.Ok, Is.True, roundTrip.Error?.Code);
            Assert.That(roundTrip.Result.ChangedMembers, Is.Empty);

            var noOp = ScriptableObjectAuthoringCommands.ImportJson(path, "{\"MonoBehaviour\":{\"Number\":3}}", confirm: true);
            Assert.That(noOp.Ok, Is.True, noOp.Error?.Code);
            Assert.That(noOp.Result.ChangedMembers, Is.Empty);
            var partial = "{\"MonoBehaviour\":{\"Number\":9}}";
            var json = ScriptableObjectAuthoringCommands.ImportJson(path, partial, confirm: true);
            Assert.That(json.Ok, Is.True, json.Error == null ? null : json.Error.Code + ":" +
                string.Join(",", json.Error.Details.Select(item => item.Key + "=" + item.Value)));
            Assert.That(json.Result.Persistent, Is.True);
            Assert.That(json.Result.DirtyBeforeSave, Is.True, "Asset was not dirty before SaveAssetIfDirty.");
            Assert.That(json.Result.DirtyAfterSave, Is.False, "Asset remained dirty after SaveAssetIfDirty.");
            Assert.That(json.Result.ChangedMembers, Is.EqualTo(new[] { "Number" }));
            Assert.That(asset.Number, Is.EqualTo(9), "Caller=" + Exact(asset) + ", command=" + json.Result.InstanceId);
            yield return null;
            StringAssert.Contains("Number: 9", File.ReadAllText(serializedPath));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<CoreAuthoredStateFixtureAsset>(path);
            Assert.That(reloaded.Number, Is.EqualTo(9));
            Assert.That(reloaded.Text, Is.EqualTo("before"));
            Assert.That(reloaded.Reference, Is.SameAs(reference));
            Undo.PerformUndo();
            Assert.That(asset.Number, Is.EqualTo(3), "Undo restores the in-memory partial mutation.");
            StringAssert.Contains("Number: 9", File.ReadAllText(serializedPath), "Undo does not rewrite the saved asset file.");

            var member = ScriptableObjectAuthoringCommands.MemberSet(path, "TextValue", "after", confirm: true);
            Assert.That(member.Ok, Is.True, member.Error?.Code);
            Assert.That(reloaded.TextValue, Is.EqualTo("after"));

            var ambiguous = ScriptableObjectAuthoringCommands.ImportJson(
                path, "{}", jsonFilePath: Root + "/input.json", confirm: true);
            Assert.That(ambiguous.Error.Code, Is.EqualTo("JSON_SOURCE_AMBIGUOUS"));
        }

        [Test]
        public void SceneUnloadRequiresDirtyDecisionAndRefusesOnlyLoadedScene()
        {
            const string fixtureScene = "Assets/Verification/Fixtures/Scenes/DomainTargets.unity";
            const string primaryPath = "Assets/Task10ScenePrimary.unity";
            const string additivePath = "Assets/Task10SceneAdditive.unity";
            var primary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.That(EditorSceneManager.SaveScene(primary, primaryPath), Is.True);
            var additive = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Assert.That(EditorSceneManager.SaveScene(additive, additivePath), Is.True);
            try
            {
                SceneManager.SetActiveScene(additive);
                new GameObject("Task10Dirty");
                EditorSceneManager.MarkSceneDirty(additive);
                Assert.That(additive.isDirty, Is.True);
                var refused = SceneAuthoringCommands.Unload(additive.handle.GetRawData().ToString());
                Assert.That(refused.Error.Code, Is.EqualTo("DIRTY_SCENE_DECISION_REQUIRED"));
                var closed = SceneAuthoringCommands.Unload(additive.handle.GetRawData().ToString(), dirtyAction: "discard");
                Assert.That(closed.Ok, Is.True, closed.Error?.Code);
                Assert.That(additive.isLoaded, Is.False);
                var only = SceneAuthoringCommands.Unload(primary.handle.GetRawData().ToString());
                Assert.That(only.Error.Code, Is.EqualTo("ONLY_LOADED_SCENE"));
            }
            finally
            {
                if (additive.IsValid() && additive.isLoaded)
                    EditorSceneManager.CloseScene(additive, true);
                if (File.Exists(fixtureScene))
                    EditorSceneManager.OpenScene(fixtureScene, OpenSceneMode.Single);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(primaryPath);
                AssetDatabase.DeleteAsset(additivePath);
            }
        }

        [Test]
        public void ExactObjectReferenceRoundTripsEntityIdWithoutSignedNarrowing()
        {
            var gameObject = new GameObject("Task10EntityIdTarget");
            var exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference.ExactId(gameObject);
            try
            {
                Assert.That(exact, Is.EqualTo(EntityId.ToULong(gameObject.GetEntityId()).ToString()));
                Assert.That(BatihanDev.UnityCliCommands.Editor.ExactObjectReference.Resolve<GameObject>(exact),
                    Is.SameAs(gameObject));
                Assert.That(BatihanDev.UnityCliCommands.Editor.ExactObjectReference.Resolve<Material>(exact), Is.Null);
                Assert.That(BatihanDev.UnityCliCommands.Editor.ExactObjectReference.Resolve<GameObject>("invalid"), Is.Null);
                Assert.That(BatihanDev.UnityCliCommands.Editor.ExactObjectReference.Resolve<GameObject>(ulong.MaxValue.ToString()), Is.Null);
            }
            finally
            {
                UnityObject.DestroyImmediate(gameObject);
            }
            Assert.That(BatihanDev.UnityCliCommands.Editor.ExactObjectReference.Resolve<GameObject>(exact), Is.Null);
        }

        private static string Exact(UnityObject value) =>
            EntityId.ToULong(value.GetEntityId()).ToString();
    }

}
