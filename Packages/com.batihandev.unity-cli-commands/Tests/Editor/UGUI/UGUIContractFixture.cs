using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using BatihanDev.UnityCliCommands.UGUI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
using UnityObject = UnityEngine.Object;
namespace BatihanDev.UnityCliCommands.Tests.UGUI
{
    public abstract class UGUIContractFixture
    {
        [SetUp] public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Selection.objects = Array.Empty<UnityObject>();
            Undo.ClearAll();
        }
        [TearDown] public void TearDown()
        {
            Selection.objects = Array.Empty<UnityObject>();
            Undo.ClearAll();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        protected static string Id(UnityObject value) => Exact.ExactId(value);
        protected static GameObject Created(CommandResult<UIResult> result)
        {
            Assert.That(result.Ok, Is.True, result.Error?.Message);
            Assert.That(result.Result.Applied, Is.True);
            var go = Exact.Resolve<GameObject>(result.Result.Target);
            Assert.That(go, Is.Not.Null);
            Assert.That(result.Result.Target, Is.EqualTo(Id(go)));
            return go;
        }
        protected static RectTransform Rect(string name, Vector2 size, Vector2 position, Vector2 pivot, UnityEngine.Transform parent = null)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.sizeDelta = size; rect.pivot = pivot; rect.anchoredPosition = position;
            return rect;
        }
        protected static int Objects() => UnityObject.FindObjectsByType<UnityEngine.Transform>(FindObjectsInactive.Include).Length;
        protected static CommandResult<UIResult> Widget(string kind, bool confirm = true, bool dryRun = false, string parent = null, string backend = "Legacy")
        {
            switch(kind)
            {
                case "Canvas": return UIWidgetCommands.Canvas(confirm:confirm,dryRun:dryRun);
                case "Panel": return UIWidgetCommands.Panel(parent:parent,confirm:confirm,dryRun:dryRun);
                case "Button": return UIWidgetCommands.Button(parent:parent,backend:backend,confirm:confirm,dryRun:dryRun);
                case "Text": return UIWidgetCommands.Text(parent:parent,backend:backend,confirm:confirm,dryRun:dryRun);
                case "Image": return UIWidgetCommands.Image(parent:parent,confirm:confirm,dryRun:dryRun);
                case "InputField": return UIWidgetCommands.InputField(parent:parent,backend:backend,confirm:confirm,dryRun:dryRun);
                case "Slider": return UIWidgetCommands.Slider(parent:parent,confirm:confirm,dryRun:dryRun);
                case "Toggle": return UIWidgetCommands.Toggle(parent:parent,backend:backend,confirm:confirm,dryRun:dryRun);
                case "Dropdown": return UIWidgetCommands.Dropdown(parent:parent,backend:backend,confirm:confirm,dryRun:dryRun);
                case "ScrollView": return UIWidgetCommands.ScrollView(parent:parent,confirm:confirm,dryRun:dryRun);
                case "RawImage": return UIWidgetCommands.RawImage(parent:parent,confirm:confirm,dryRun:dryRun);
                case "Scrollbar": return UIWidgetCommands.Scrollbar(parent:parent,confirm:confirm,dryRun:dryRun);
                default: throw new ArgumentException(kind);
            }
        }
    }
}
