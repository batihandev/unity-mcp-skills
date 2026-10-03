using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using UnityEditor;
using UnityEngine;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class SmartObjectCommands
    {
        [CliCommand("smart.replace", "Smart replace operation.", Tags = new[] { "unity-cli-commands", "smart" })]
        public static CommandResult<SmartReplacementReport> Replace([CliArg("prefabPath", "Existing confined Assets prefab asset path.")] string prefabPath, [CliArg("dryRun", "Preview only; wins over confirmation and preserves authored state.")] bool dryRun = true, [CliArg("confirm", "Commit the reviewed operation only when dryRun=false.")] bool confirm = false, [CliArg("targetsJson", "Optional JSON array of exact GameObject references; omitted uses current Selection.")] string targetsJson = null)
        {
            const string schema = "unity.smart.replace@1";
            try
            {
                SmartSelectionFacts.Authoring(dryRun, confirm);
                var safe = ProjectPathPolicy.Validate(prefabPath);
                if (!safe.Ok) return CommandResult<SmartReplacementReport>.Failure(schema, safe.Error);
                if (!safe.Result.Path.StartsWith("Assets/", StringComparison.Ordinal) || !safe.Result.Exists || !safe.Result.Path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    throw new SmartFailure("SMART_PREFAB_INVALID", "Choose an existing confined Assets prefab path.");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(safe.Result.Path);
                if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab) || PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
                    throw new SmartFailure("SMART_PREFAB_INVALID", "The path must identify an eligible prefab asset root.");
                var originals = SmartSelectionFacts.Capture(targetsJson);
                if (originals.Any(go => PrefabUtility.IsPartOfPrefabInstance(go) && !PrefabUtility.IsOutermostPrefabInstanceRoot(go) && !PrefabUtility.IsAddedGameObjectOverride(go)))
                    throw new SmartFailure("SMART_TARGET_INELIGIBLE", "Select the whole outermost prefab instance root; connected template children and nested instance objects cannot be replaced individually.");
                var snapshots = originals.Select(go => new ReplacementSnapshot
                {
                    Original = go, OriginalId = Exact.ExactId(go), Parent = go.transform.parent, Position = go.transform.position, Rotation = go.transform.rotation,
                    Scale = go.transform.localScale, Sibling = go.transform.GetSiblingIndex(), Scene = go.scene
                }).ToList();
                var report = new SmartReplacementReport { Preview = dryRun, SelectedCount = originals.Count, PrefabPath = safe.Result.Path, SelectionUndoable = false, UndoGroup = -1,
                    Replacements = snapshots.Select(value => new SmartReplacementItem { Original = value.OriginalId, Parent = Exact.ExactId(value.Parent), ScenePath = value.Scene.path, SiblingIndex = value.Sibling }).ToList() };
                if (dryRun) return CommandResult<SmartReplacementReport>.Success(schema, report);
                var priorSelection = Selection.objects;
                using (var scope = new AuthoringUndoScope("Smart Replace"))
                {
                    report.UndoGroup = Undo.GetCurrentGroup(); var replacements = new List<GameObject>();
                    try
                    {
                        foreach (var snapshot in snapshots)
                        {
                            var replacement = PrefabUtility.InstantiatePrefab(prefab, snapshot.Scene) as GameObject;
                            if (replacement == null) throw new InvalidOperationException("Prefab instantiation did not return a scene GameObject.");
                            Undo.RegisterCreatedObjectUndo(replacement, "Smart Replace");
                            if (snapshot.Parent != null) Undo.SetTransformParent(replacement.transform, snapshot.Parent, "Smart Replace");
                            Undo.RecordObject(replacement.transform, "Smart Replace");
                            replacement.transform.position = snapshot.Position; replacement.transform.rotation = snapshot.Rotation; replacement.transform.localScale = snapshot.Scale;
                            Undo.SetSiblingIndex(replacement.transform, snapshot.Sibling, "Smart Replace");
                            Undo.DestroyObjectImmediate(snapshot.Original);
                            SmartSelectionFacts.Changed(replacement.transform, replacement);
                            report.Replacements[replacements.Count].Replacement = Exact.ExactId(replacement); replacements.Add(replacement); report.ReplacedCount++;
                        }
                        Undo.FlushUndoRecordObjects(); Selection.objects = replacements.Cast<UnityEngine.Object>().ToArray();
                        return CommandResult<SmartReplacementReport>.Success(schema, report);
                    }
                    catch (Exception failure)
                    {
                        Exception rollbackFailure = null; Undo.FlushUndoRecordObjects();
                        try { Undo.RevertAllDownToGroup(report.UndoGroup); scope.Cancel(); } catch (Exception rollback) { rollbackFailure = rollback; scope.Cancel(); }
                        Selection.objects = priorSelection.Where(value => value != null).ToArray();
                        var restored = rollbackFailure == null && snapshots.All(value => value.Original != null && value.Original.scene == value.Scene && value.Original.transform.parent == value.Parent && value.Original.transform.GetSiblingIndex() == value.Sibling && SmartSelectionFacts.TransformMatches(value.Original, value.Position, value.Rotation, value.Scale)) && replacements.All(value => value == null);
                        throw new SmartFailure("SMART_REPLACEMENT_FAILED", "Prefab replacement failed; inspect structural rollback and actual Selection before further authoring.", new Dictionary<string, object> { ["targetRollback"] = restored, ["selectionRestored"] = Selection.objects.SequenceEqual(priorSelection.Where(value => value != null)), ["failure"] = failure.Message, ["rollbackFailure"] = rollbackFailure?.Message });
                    }
                }
            }
            catch (Exception failure) { return SmartSelectionFacts.Failure<SmartReplacementReport>(schema, failure); }
        }
        private sealed class ReplacementSnapshot
        {
            internal GameObject Original;
            internal string OriginalId;
            internal UnityEngine.Transform Parent;
            internal Vector3 Position;
            internal Quaternion Rotation;
            internal Vector3 Scale;
            internal int Sibling;
            internal UnityEngine.SceneManagement.Scene Scene;
        }
    }
}
