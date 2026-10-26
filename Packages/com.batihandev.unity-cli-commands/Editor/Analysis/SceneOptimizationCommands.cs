using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using UnityEditor;
using UnityEngine;
using Unity.Pipeline.Commands;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class SceneOptimizationCommands
    {
        [CliCommand("optimization.static-set", "Set named static flags on one exact object or its complete subtree with Undo.", Tags = new[] { "unity-cli-commands", "optimization" })]
        public static CommandResult<SceneMutationReport> StaticSet([CliArg("target", "Exact GameObject reference.", Required = true)] string target,
            [CliArg("flags", "Named comma-separated StaticEditorFlags, Everything or Nothing.")] string flags = "Everything",
            [CliArg("includeChildren", "Include inactive children, deduplicating the root.")] bool includeChildren = false,
            [CliArg("confirm", "Apply the reviewed operation.")] bool confirm = false,
            [CliArg("dryRun", "Preview without mutation; wins over confirmation.")] bool dryRun = false)
        {
            const string schema = "unity.optimization.static-set@1";
            var compatibility = CompatibilityPolicy.CheckInstalled(); if (!compatibility.Ok) return CommandResult<SceneMutationReport>.Failure(schema, compatibility.Error);
            if (!TryFlags(flags, out var requested)) return Fail(schema, "STATIC_FLAGS_INVALID", "Supply only named static flags, Everything or Nothing.");
            var go = Resolve(target); if (go == null) return Fail(schema, "SCENE_TARGET_INVALID", "Select an exact ordinary loaded scene GameObject.");
            if (!dryRun && !confirm) return Fail(schema, "CONFIRMATION_REQUIRED", "Preview with dryRun=true or apply with confirm=true.");
            var selected = includeChildren ? go.GetComponentsInChildren<UnityEngine.Transform>(true).Select(transform => transform.gameObject).Distinct().ToArray() : new[] { go };
            var original = selected.Select(GameObjectUtility.GetStaticEditorFlags).ToArray();
            var report = new SceneMutationReport { Target = Exact.ExactId(go), Targets = selected.Select(Exact.ExactId).ToList(), SelectedCount = selected.Length, DryRun = dryRun };
            if (dryRun) return CommandResult<SceneMutationReport>.Success(schema, report);
            using (var scope = new Unity.Pipeline.Editor.Authoring.AuthoringUndoScope("Set Static Flags"))
            {
                var group = Undo.GetCurrentGroup();
                try
                {
                    Undo.RegisterCompleteObjectUndo(selected.Cast<UnityEngine.Object>().ToArray(), "Set Static Flags");
                    for (var index = 0; index < selected.Length; index++)
                    {
                        GameObjectUtility.SetStaticEditorFlags(selected[index], requested);
                        if (GameObjectUtility.GetStaticEditorFlags(selected[index]) != requested) throw new InvalidOperationException("Static flags did not match the requested flags.");
                        report.AppliedCount++;
                    }
                    Undo.FlushUndoRecordObjects(); report.Applied = true; return CommandResult<SceneMutationReport>.Success(schema, report);
                }
                catch (Exception exception)
                {
                    try { Undo.RevertAllDownToGroup(group); } catch (Exception rollback) { return MutationFailure(schema, exception, report.AppliedCount, false, rollback); }
                    var restored = selected.Select(GameObjectUtility.GetStaticEditorFlags).SequenceEqual(original);
                    return MutationFailure(schema, exception, report.AppliedCount, restored);
                }
            }
        }
        [CliCommand("optimization.lod-setup", "Create or configure exact LODGroup levels and bounds with one Undo group.", Tags = new[] { "unity-cli-commands", "optimization" })]
        public static CommandResult<SceneMutationReport> LodSetup([CliArg("target", "Exact GameObject reference.", Required = true)] string target,
            [CliArg("lodDistances", "Finite strictly descending heights in (0,1], with an appended culled zero.")] string lodDistances = "0.6,0.3,0.1",
            [CliArg("confirm", "Apply the reviewed operation.")] bool confirm = false,
            [CliArg("dryRun", "Preview without mutation; wins over confirmation.")] bool dryRun = false)
        {
            const string schema = "unity.optimization.lod-setup@1";
            var compatibility = CompatibilityPolicy.CheckInstalled(); if (!compatibility.Ok) return CommandResult<SceneMutationReport>.Failure(schema, compatibility.Error);
            var distances = new List<float>();
            if (string.IsNullOrWhiteSpace(lodDistances)) return Fail(schema, "LOD_DISTANCES_INVALID", "Supply finite strictly descending heights in (0,1].");
            foreach (var part in lodDistances.Split(','))
            {
                if (!float.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || float.IsNaN(value) || float.IsInfinity(value) || value <= 0 || value > 1 || (distances.Count > 0 && value >= distances[distances.Count - 1]))
                    return Fail(schema, "LOD_DISTANCES_INVALID", "Supply finite strictly descending heights in (0,1].");
                distances.Add(value);
            }
            var go = Resolve(target); if (go == null) return Fail(schema, "SCENE_TARGET_INVALID", "Select an exact ordinary loaded scene GameObject.");
            if (!dryRun && !confirm) return Fail(schema, "CONFIRMATION_REQUIRED", "Preview with dryRun=true or apply with confirm=true.");
            var renderers = go.GetComponentsInChildren<Renderer>();
            var group = go.GetComponent<LODGroup>();
            var original = group == null ? null : group.GetLODs(); var originalSize = group == null ? 0 : group.size; var originalPoint = group == null ? Vector3.zero : group.localReferencePoint;
            var report = new SceneMutationReport { Target = Exact.ExactId(go), Targets = new List<string> { Exact.ExactId(go) }, SelectedCount = 1, DryRun = dryRun, Distances = distances.Concat(new[] { 0f }).ToArray(), RendererCount = renderers.Length };
            if (dryRun) return CommandResult<SceneMutationReport>.Success(schema, report);
            using (var scope = new Unity.Pipeline.Editor.Authoring.AuthoringUndoScope("Configure LOD Group"))
            {
                var undoGroup = Undo.GetCurrentGroup();
                try
                {
                    if (group == null) group = Undo.AddComponent<LODGroup>(go); else Undo.RegisterCompleteObjectUndo(group, "Configure LOD Group");
                    var lods = report.Distances.Select((distance, index) => new LOD(distance, index == 0 ? renderers : new Renderer[0])).ToArray();
                    group.SetLODs(lods); group.RecalculateBounds();
                    Undo.FlushUndoRecordObjects(); report.Applied = true; report.AppliedCount = 1;
                    return CommandResult<SceneMutationReport>.Success(schema, report);
                }
                catch (Exception exception)
                {
                    try { Undo.RevertAllDownToGroup(undoGroup); } catch (Exception rollback) { return MutationFailure(schema, exception, report.AppliedCount, false, rollback); }
                    var observed = go.GetComponent<LODGroup>();
                    var restored = original == null ? observed == null : observed != null && observed.size == originalSize && observed.localReferencePoint == originalPoint && observed.GetLODs().Select(lod => lod.screenRelativeTransitionHeight).SequenceEqual(original.Select(lod => lod.screenRelativeTransitionHeight)) && observed.GetLODs().SelectMany(lod => lod.renderers).SequenceEqual(original.SelectMany(lod => lod.renderers));
                    return MutationFailure(schema, exception, report.AppliedCount, restored);
                }
            }
        }
        private static GameObject Resolve(string reference)
        {
            var go = Exact.Resolve<GameObject>(reference); return go != null && AnalysisSceneObjects.Loaded().Contains(go) ? go : null;
        }
        private static bool TryFlags(string text, out StaticEditorFlags flags)
        {
            flags = 0; if (string.IsNullOrWhiteSpace(text)) return false;
            foreach (var part in text.Split(','))
            {
                var name = part.Trim(); if (string.Equals(name, "Everything", StringComparison.OrdinalIgnoreCase)) { flags |= (StaticEditorFlags)(-1); continue; }
                if (string.Equals(name, "Nothing", StringComparison.OrdinalIgnoreCase)) continue;
                var declared = Enum.GetNames(typeof(StaticEditorFlags)).FirstOrDefault(value => string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
                if (declared == null) return false; flags |= (StaticEditorFlags)Enum.Parse(typeof(StaticEditorFlags), declared);
            }
            return true;
        }
        private static CommandResult<SceneMutationReport> Fail(string schema, string code, string message) => CommandResult<SceneMutationReport>.Failure(schema, code, message);
        private static CommandResult<SceneMutationReport> MutationFailure(string schema, Exception exception, int completed, bool rolledBack, Exception rollback = null) =>
            CommandResult<SceneMutationReport>.Failure(schema, "SCENE_MUTATION_FAILED", "The operation failed; inspect rollback evidence before further authoring.", new Dictionary<string, object>
            { ["completedBeforeFailure"] = completed, ["rolledBack"] = rolledBack, ["exceptionType"] = exception.GetType().Name, ["message"] = exception.Message, ["rollbackError"] = rollback?.Message });
    }
}
