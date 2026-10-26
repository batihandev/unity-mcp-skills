using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Editor.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
using UnityComponent = UnityEngine.Component;
using UnityObject = UnityEngine.Object;
namespace BatihanDev.UnityCliCommands.UGUI
{
    internal static class UISceneAuthoring
    {
        internal static string Schema(string command) => "unity.ui." + command + "@1";
        internal static void Require(bool condition, string message)
        {
            if (!condition)
                throw new ArgumentException(message);
        }
        internal static void Finite(params float[] values) => Require(
            values.All(v => !float.IsNaN(v) && !float.IsInfinity(v)), "All numbers must be finite.");
        internal static void Size(float width, float height)
        {
            Finite(width, height);
            Require(width > 0 && height > 0, "Dimensions must be positive.");
        }
        internal static void Color(float r, float g, float b, float a = 1)
        {
            Finite(r, g, b, a);
            Require(new[] { r, g, b, a }.All(v => v >= 0 && v <= 1), "Color channels must be in [0,1].");
        }
        internal static T Named<T>(string value)
            where T : struct
        {
            var name = Enum.GetNames(typeof(T)).SingleOrDefault(
                n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));
            Require(name != null, "Supply a named " + typeof(T).Name + " value.");
            return (T)Enum.Parse(typeof(T), name);
        }
        internal static bool Regular(GameObject go) =>
            go != null && !EditorUtility.IsPersistent(go) && go.scene.IsValid() && go.scene.isLoaded &&
            !EditorSceneManager.IsPreviewSceneObject(go) && PrefabStageUtility.GetPrefabStage(go) == null;
        internal static T[] All<T>()
            where T : UnityComponent => UnityObject.FindObjectsByType<T>(FindObjectsInactive.Include).Where(c => Regular(c.gameObject)).OrderBy(c => Exact.ExactId(c),
                      StringComparer.Ordinal).ToArray();
        internal static GameObject Object(string reference)
        {
            var go = Exact.Resolve<GameObject>(reference);
            Require(Regular(go), "Supply an exact GameObject in a loaded regular scene.");
            return go;
        }
        internal static GameObject Parent(string reference)
        {
            if (reference != null)
                return Object(reference);
            var canvases = All<Canvas>();
            Require(canvases.Length <= 1, "Multiple Canvases require an exact parent.");
            return canvases.SingleOrDefault()?.gameObject;
        }
        internal static RectTransform Rect(string reference)
        {
            var rect = Object(reference).GetComponent<RectTransform>();
            Require(rect != null, "Target requires RectTransform.");
            return rect;
        }
        internal static RectTransform[] Targets(string references, int minimum)
        {
            var objects = references == null
                              ? Selection.gameObjects
                              : references.Split(',').Select(value => Object(value.Trim())).ToArray();
            var rects = objects.Where(go => references != null || go.GetComponent<RectTransform>() != null)
                            .Select(go => Rect(Exact.ExactId(go)))
                            .ToArray();
            Require(rects.Distinct().Count() == rects.Length, "Targets must be distinct.");
            Require(rects.Length >= minimum, "Select at least " + minimum + " distinct RectTransforms.");
            return rects;
        }
        internal static string Path(UnityEngine.Transform transform) => transform.parent == null
                                                                            ? transform.name
                                                                            : Path(transform.parent) +
                                                                                  "/" + transform.name;
        internal static void Record(GameObject go, string command)
        {
            Undo.RegisterCompleteObjectUndo(go.GetComponentsInChildren<UnityComponent>(true)
                                                .Where(c => c != null)
                                                .Cast<UnityObject>()
                                                .ToArray(),
                command);
        }
        internal static CommandResult<UIResult> Run(string command, bool confirm, bool dryRun,
            Func<GameObject> validate, Action apply, Func<UIResult> read)
        {
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok)
                return CommandResult<UIResult>.Failure(Schema(command), compatibility.Error);
            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode, "UI authoring requires Edit mode.");
                Require(PrefabStageUtility.GetCurrentPrefabStage() == null,
                    "Close Prefab Stage before UI authoring.");
                var target = validate();
                var scene = target == null ? SceneManager.GetActiveScene() : target.scene;
                Require(scene.IsValid() && scene.isLoaded && !EditorSceneManager.IsPreviewScene(scene),
                    "A loaded regular scene is required.");
                Require(confirm || dryRun, "Set confirm=true or dryRun=true.");
                if (dryRun)
                    return CommandResult<UIResult>.Success(
                        Schema(command), new UIResult { Target = Exact.ExactId(target), Applied = false,
                            DryRun = true, State = new Dictionary<string, object>() });
                using (var scope = new AuthoringUndoScope("ui." + command))
                {
                    var group = Undo.GetCurrentGroup();
                    try
                    {
                        apply();
                        var result = read();
                        var authored = Exact.Resolve<GameObject>(result.Target);
                        if (authored != null)
                            foreach (var component in authored.GetComponentsInChildren<UnityComponent>(true))
                                if (component != null)
                                {
                                    EditorUtility.SetDirty(component);
                                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                                }
                        EditorSceneManager.MarkSceneDirty(scene);
                        Undo.FlushUndoRecordObjects();
                        result.Applied = true;
                        return CommandResult<UIResult>.Success(Schema(command), result);
                    }
                    catch
                    {
                        Undo.RevertAllDownToGroup(group);
                        scope.Cancel();
                        throw;
                    }
                }
            }
            catch (Exception exception)
            {
                return CommandResult<UIResult>.Failure(
                    Schema(command), "UI_REQUEST_INVALID", exception.Message);
            }
        }
    }
}
