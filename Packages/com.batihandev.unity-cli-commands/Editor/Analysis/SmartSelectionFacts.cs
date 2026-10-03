using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.Analysis
{
    internal sealed class SmartFailure : Exception
    {
        internal string Code { get; }
        internal Dictionary<string, object> Details { get; }
        internal SmartFailure(string code, string message, Dictionary<string, object> details = null) : base(message) { Code = code; Details = details; }
    }
    internal static class SmartSelectionFacts
    {
        internal static void Compatible()
        {
            var compatible = CompatibilityPolicy.CheckInstalled();
            if (!compatible.Ok) throw new SmartFailure(compatible.Error.Code, compatible.Error.Message, compatible.Error.Details);
        }
        internal static void Authoring(bool dryRun = true, bool confirm = false)
        {
            Compatible();
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new SmartFailure("SMART_EDITOR_BUSY", "Wait for an idle Edit-mode Editor before Smart authoring.");
            if (!dryRun && !confirm) throw new SmartFailure("SMART_CONFIRM_REQUIRED", "Preview with dryRun=true, then apply the reviewed operation with dryRun=false and confirm=true.");
        }
        internal static IReadOnlyList<GameObject> Ordinary(bool inactive = true) => AnalysisSceneObjects.Loaded(inactive, true);
        internal static List<GameObject> Capture(string json, int minimum = 1, bool siblingOrder = false)
        {
            GameObject[] candidates;
            if (json == null) candidates = Selection.gameObjects;
            else
            {
                JArray array;
                try { array = JArray.Parse(json); }
                catch (Exception) { throw new SmartFailure("SMART_INPUT_INVALID", "Supply targetsJson as an array of exact reference strings."); }
                if (array.Any(token => token.Type != JTokenType.String)) throw new SmartFailure("SMART_INPUT_INVALID", "Every targetsJson element must be an exact reference string.");
                candidates = array.Select(token => Exact.Resolve<GameObject>((string)token)).ToArray();
            }
            if (candidates.Any(go => go == null)) throw new SmartFailure("SMART_TARGET_NOT_FOUND", "A selected reference is missing or stale; resolve the whole set again.");
            var selected = candidates.Distinct().ToList();
            if (selected.Count < minimum) throw new SmartFailure("SMART_SELECTION_INVALID", "Select at least " + minimum.ToString(CultureInfo.InvariantCulture) + " independent scene GameObjects.");
            var eligible = Ordinary();
            if (selected.Any(go => !eligible.Contains(go) || EditorUtility.IsPersistent(go))) throw new SmartFailure("SMART_TARGET_INELIGIBLE", "Select only ordinary loaded scene GameObjects, outside preview and prefab stages.");
            for (var i = 0; i < selected.Count; i++)
                for (var j = i + 1; j < selected.Count; j++)
                    if (selected[i].transform.IsChildOf(selected[j].transform) || selected[j].transform.IsChildOf(selected[i].transform))
                        throw new SmartFailure("SMART_SELECTION_OVERLAP", "Select independent objects; an ancestor and its descendant cannot be authored together.");
            foreach (var go in selected) RequireFinite(go.transform.position, go.transform.rotation, go.transform.localScale);
            return siblingOrder ? selected.OrderBy(go => go.transform.GetSiblingIndex()).ToList() : selected;
        }
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        internal static bool Finite(Quaternion value) => Finite(value.x) && Finite(value.y) && Finite(value.z) && Finite(value.w);
        internal const float PositionTolerance = .0002f;
        internal const float RotationToleranceDegrees = .02f;
        internal static bool TransformMatches(GameObject value, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (value == null) return false;
            var transform = value.transform;
            return Finite(transform.position) && Finite(transform.rotation) && Finite(transform.localScale) &&
                Vector3.Distance(transform.position, position) < PositionTolerance &&
                Quaternion.Angle(transform.rotation, rotation) < RotationToleranceDegrees &&
                Vector3.Distance(transform.localScale, scale) < PositionTolerance;
        }
        internal static void RequireFinite(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!Finite(position) || !Finite(rotation) || !Finite(scale)) throw new SmartFailure("SMART_INPUT_INVALID", "All authored transform values must remain finite.");
        }
        internal static void Nonnegative(float value, string name)
        { if (!Finite(value) || value < 0) throw new SmartFailure("SMART_INPUT_INVALID", name + " must be finite and nonnegative."); }
        internal static void Positive(float value, string name)
        { if (!Finite(value) || value <= 0) throw new SmartFailure("SMART_INPUT_INVALID", name + " must be finite and positive."); }
        internal static Vector3 Axis(string axis)
        {
            switch (axis?.ToUpperInvariant())
            {
                case "X": return Vector3.right; case "-X": return Vector3.left;
                case "Y": return Vector3.up; case "-Y": return Vector3.down;
                case "Z": return Vector3.forward; case "-Z": return Vector3.back;
                default: throw new SmartFailure("SMART_INPUT_INVALID", "Choose X, Y, Z, -X, -Y or -Z.");
            }
        }
        internal static void Changed(UnityEngine.Object value, GameObject owner)
        {
            EditorUtility.SetDirty(value);
            if (PrefabUtility.IsPartOfPrefabInstance(value)) PrefabUtility.RecordPrefabInstancePropertyModifications(value);
            if (owner.scene.IsValid()) EditorSceneManager.MarkSceneDirty(owner.scene);
        }
        internal static CommandResult<T> Failure<T>(string schema, Exception exception)
        {
            if (exception is SmartFailure refusal) return CommandResult<T>.Failure(schema, refusal.Code, refusal.Message, refusal.Details);
            return CommandResult<T>.Failure(schema, "SMART_OPERATION_FAILED", "Smart operation failed; inspect the error before further authoring.", new Dictionary<string, object> { ["exception"] = exception.GetType().Name, ["message"] = exception.Message });
        }
    }
}
