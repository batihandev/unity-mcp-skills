using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using UnityEditor;
using UnityEngine;
using Unity.Pipeline.Commands;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class MissingScriptCommands
    {
        [CliCommand("validation.missing-scripts-fix", "Preview or remove actual missing scene scripts with full hierarchy Undo.", Tags = new[] { "unity-cli-commands", "validation" })]
        public static CommandResult<SceneMutationReport> Fix([CliArg("includeInactive", "Include inactive ordinary scene objects.")] bool includeInactive = true,
            [CliArg("confirm", "Apply the reviewed removal.")] bool confirm = false,
            [CliArg("dryRun", "Preview only; wins over confirmation.")] bool dryRun = true)
        {
            const string schema = "unity.validation.missing-scripts-fix@1";
            var compatibility = CompatibilityPolicy.CheckInstalled(); if (!compatibility.Ok) return CommandResult<SceneMutationReport>.Failure(schema, compatibility.Error);
            if (!dryRun && !confirm) return CommandResult<SceneMutationReport>.Failure(schema, "CONFIRMATION_REQUIRED", "Preview with dryRun=true or apply with confirm=true.");
            GameObject[] targets; int[] counts;
            try { targets = (includeInactive ? AnalysisSceneObjects.Loaded(true, true) : AnalysisSceneObjects.NativeGameObjects()).Where(go => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0).ToArray(); counts = targets.Select(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount).ToArray(); }
            catch (Exception exception) { return Failed(exception, 0, false); }
            var result = new SceneMutationReport { DryRun = dryRun, SelectedCount = targets.Length, Targets = targets.Select(Exact.ExactId).ToList() };
            if (dryRun || targets.Length == 0) return CommandResult<SceneMutationReport>.Success(schema, result);
            using (var scope = new Unity.Pipeline.Editor.Authoring.AuthoringUndoScope("Remove Missing Scripts"))
            {
                var group = Undo.GetCurrentGroup();
                try
                {
                    foreach (var target in targets) Undo.RegisterFullObjectHierarchyUndo(target, "Remove Missing Scripts");
                    for (var index = 0; index < targets.Length; index++)
                    {
                        var removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(targets[index]);
                        result.RemovedComponents += removed;
                        if (removed != counts[index] || GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(targets[index]) != 0) throw new InvalidOperationException("Missing-script removal did not match the prevalidated component count.");
                        result.AppliedCount++;
                    }
                    Undo.FlushUndoRecordObjects(); result.Applied = true; return CommandResult<SceneMutationReport>.Success(schema, result);
                }
                catch (Exception exception)
                {
                    try { Undo.RevertAllDownToGroup(group); return Failed(exception, result.RemovedComponents, targets.Select(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount).SequenceEqual(counts)); }
                    catch (Exception rollback) { return CommandResult<SceneMutationReport>.Failure(schema, "SCENE_MUTATION_FAILED", "Missing-script removal and rollback failed; inspect the scene before further authoring.", new Dictionary<string, object> { ["removedBeforeFailure"] = result.RemovedComponents, ["rolledBack"] = false, ["message"] = exception.Message, ["rollbackError"] = rollback.Message }); }
                }
            }
        }
        private static CommandResult<SceneMutationReport> Failed(Exception exception, int removed, bool restored) => CommandResult<SceneMutationReport>.Failure("unity.validation.missing-scripts-fix@1", "SCENE_MUTATION_FAILED", "Missing-script removal failed; inspect rollback evidence before further authoring.",
            new Dictionary<string, object> { ["removedBeforeFailure"] = removed, ["rolledBack"] = restored, ["exceptionType"] = exception.GetType().Name, ["message"] = exception.Message });
    }
}
