using System;
using System.Linq;
using System.Reflection;
using BatihanDev.UnityCliCommands.Editor;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class SupportContextContractTests
    {
        private UnityObject[] previousSelection;
        private GameObject parent;
        private GameObject selected;
        [SetUp] public void SetUp()
        {
            previousSelection = Selection.objects;
            parent = new GameObject("SupportContextParent");
            selected = new GameObject("Duplicate", typeof(BoxCollider));
            selected.transform.SetParent(parent.transform);
            Selection.objects = new UnityObject[] { selected };
        }
        [TearDown] public void TearDown()
        {
            Selection.objects = previousSelection;
            UnityObject.DestroyImmediate(parent);
        }
        [Test] public void DefaultFlagsAreFalseAndOmitOptionalLists()
        {
            var method = typeof(EditorCommands).GetMethod(nameof(EditorCommands.ContextDetails));
            foreach (var name in new[] { "includeComponents", "includeChildren" })
            {
                var parameter = method.GetParameters().SingleOrDefault(item => item.Name == name);
                Assert.That(parameter, Is.Not.Null, "Missing optional " + name);
                Assert.That(parameter.IsOptional, Is.True);
                Assert.That(parameter.DefaultValue, Is.EqualTo(false));
            }
            var item = Read()["SelectedGameObjects"][0];
            Assert.That(item["Components"], Is.Null);
            Assert.That(item["Children"], Is.Null);
        }
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void FlagsIndependentlyProjectRealComponentsAndDirectInactiveChildren(bool components, bool children)
        {
            var child = new GameObject("Duplicate");
            child.transform.SetParent(selected.transform);
            child.SetActive(false);
            var grandchild = new GameObject("Grandchild");
            grandchild.transform.SetParent(child.transform);
            var item = Read(components, children)["SelectedGameObjects"][0];
            if (components)
            {
                Assert.That(item["Components"], Is.TypeOf<JArray>(), "Requested component projection must serialize as an array.");
                Assert.That(item["Components"].Values<string>(), Is.EqualTo(new[] { "Transform", "BoxCollider" }));
            }
            else Assert.That(item["Components"], Is.Null);
            if (children)
            {
                Assert.That(item["Children"], Is.TypeOf<JArray>(), "Requested direct-child projection must serialize as an array.");
                Assert.That(item["Children"].Count(), Is.EqualTo(1));
                Assert.That((string)item["Children"][0]["Name"], Is.EqualTo("Duplicate"));
                Assert.That((string)item["Children"][0]["InstanceId"], Is.EqualTo(Exact(child)));
            }
            else Assert.That(item["Children"], Is.Null);
        }
        [Test] public void EmptySelectionSerializesEmptyArraysAndRequestedEmptyChildren()
        {
            Selection.objects = Array.Empty<UnityObject>();
            var empty = Read();
            Assert.That(empty["SelectedGameObjects"], Is.TypeOf<JArray>());
            Assert.That(empty["SelectedGameObjects"].Count(), Is.Zero);
            Assert.That(empty["SelectedAssets"], Is.TypeOf<JArray>());
            Assert.That(empty["SelectedAssets"].Count(), Is.Zero);
            Selection.objects = new UnityObject[] { selected };
            Assert.That(Read(false, true)["SelectedGameObjects"][0]["Children"], Is.TypeOf<JArray>());
            Assert.That(Read(false, true)["SelectedGameObjects"][0]["Children"].Count(), Is.Zero);
        }
        [Test] public void MixedSelectionKeepsExactDuplicateIdentitiesAndAssetMetadata()
        {
            var other = new GameObject("Duplicate");
            other.transform.SetParent(parent.transform);
            var folder = AssetDatabase.LoadMainAssetAtPath("Assets");
            Selection.objects = new UnityObject[] { selected, other, folder };
            var result = Read();
            var objects = result["SelectedGameObjects"].ToArray();
            Assert.That(objects.Select(item => (string)item["InstanceId"]), Is.EquivalentTo(new[] { Exact(selected), Exact(other) }));
            foreach (var item in objects)
            {
                Assert.That((string)item["Name"], Is.EqualTo("Duplicate"));
                Assert.That((string)item["Tag"], Is.EqualTo("Untagged"));
                Assert.That((int)item["LayerIndex"], Is.Zero);
                Assert.That((string)item["LayerName"], Is.EqualTo("Default"));
                Assert.That((bool)item["ActiveSelf"], Is.True);
                Assert.That((bool)item["ActiveInHierarchy"], Is.True);
            }
            var asset = result["SelectedAssets"].Single(item => (string)item["Path"] == "Assets");
            Assert.That((string)asset["Guid"], Is.EqualTo(AssetDatabase.AssetPathToGUID("Assets")));
            Assert.That((string)asset["InstanceId"], Is.EqualTo(Exact(folder)));
            Assert.That((bool)asset["IsFolder"], Is.True);
            Assert.That((string)result["FocusedWindow"], Is.EqualTo(EditorWindow.focusedWindow == null ? "None" : EditorWindow.focusedWindow.GetType().Name));
        }
        [Test] public void ComponentProjectionSkipsMissingScriptsAndDoesNotMutateSelectionOrDirtyState()
        {
            var component = selected.AddComponent<AnalysisReferenceFixture>();
            using (var serialized = new SerializedObject(component))
            {
                serialized.FindProperty("m_Script").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(selected), Is.EqualTo(1));
            selected.SetActive(false);
            var selection = Selection.objects;
            var dirty = selected.scene.isDirty;
            var json = Read(true, true);
            Assert.That(json["SelectedGameObjects"][0]["Components"], Is.TypeOf<JArray>(), "Missing scripts must not suppress the requested projection.");
            Assert.That(json["SelectedGameObjects"][0]["Components"].Values<string>(), Is.EqualTo(new[] { "Transform", "BoxCollider" }));
            Assert.That((bool)json["SelectedGameObjects"][0]["ActiveSelf"], Is.False);
            Assert.That((bool)json["SelectedGameObjects"][0]["ActiveInHierarchy"], Is.False);
            Assert.That(Selection.objects, Is.EqualTo(selection));
            Assert.That(selected.scene.isDirty, Is.EqualTo(dirty));
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(selected), Is.EqualTo(1));
        }
        private static JObject Read(bool components = false, bool children = false)
        {
            var method = typeof(EditorCommands).GetMethod(nameof(EditorCommands.ContextDetails));
            var parameters = method.GetParameters();
            object value;
            if (parameters.Length == 0) value = method.Invoke(null, null);
            else value = method.Invoke(null, parameters.Select(parameter => parameter.Name == "includeComponents" ? (object)components : parameter.Name == "includeChildren" ? children : parameter.DefaultValue).ToArray());
            var result = (CommandResult<EditorContextDetailsResult>)value;
            Assert.That(result.Ok, Is.True, result.Error?.Code);
            return JObject.Parse(JsonConvert.SerializeObject(result.Result));
        }
        private static string Exact(UnityObject value) => EntityId.ToULong(value.GetEntityId()).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
