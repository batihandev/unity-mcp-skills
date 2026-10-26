using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using UnityEditor;
using Unity.Pipeline.Commands;
using UnityEngine;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class MaterialAnalysisCommands
    {
        [CliCommand("analysis.materials-equivalent", "Group approximate shader, color and queue equivalence; texture identity is ignored.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<MaterialEquivalentReport> Equivalent([CliArg("limit", "Maximum shown equivalent-material groups.")] int limit = 50)
        {
            const string schema = "unity.analysis.materials-equivalent@1";
            var compatibility = CompatibilityPolicy.CheckInstalled(); if (!compatibility.Ok) return CommandResult<MaterialEquivalentReport>.Failure(schema, compatibility.Error);
            if (limit < 0) return CommandResult<MaterialEquivalentReport>.Failure(schema, "ANALYSIS_INPUT_INVALID", "Use a nonnegative group limit.");
            try
            {
                var groups = new Dictionary<(string shader, string color, int queue), MaterialEquivalentGroup>();
                foreach (var guid in AssetDatabase.FindAssets("t:Material"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var material = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
                    if (material == null || material.shader == null) continue;
                    var color = material.HasColor("_Color") ? material.GetColor("_Color").ToString()
                        : material.HasColor("_BaseColor") ? material.GetColor("_BaseColor").ToString() : "none";
                    var key = (material.shader.name, color, material.renderQueue);
                    if (!groups.TryGetValue(key, out var group)) groups[key] = group = new MaterialEquivalentGroup { Shader = key.Item1, Color = color, RenderQueue = material.renderQueue };
                    group.Materials.Add(AssetAnalysisFacts.Describe(path));
                }
                var equivalent = groups.Values.Where(group => group.Materials.Count > 1).ToList();
                return CommandResult<MaterialEquivalentReport>.Success(schema, new MaterialEquivalentReport { Groups = equivalent.Take(limit).ToList(), Total = equivalent.Count,
                    Truncated = equivalent.Count > limit, Note = "Comparison is approximate: shader name, color representation and render queue only. All texture identities and other material properties are ignored." });
            }
            catch (Exception exception) { return CommandResult<MaterialEquivalentReport>.Failure(schema, "ANALYSIS_READ_FAILED", "Material analysis failed; inspect the reported error before retrying.", new Dictionary<string, object> { ["exceptionType"] = exception.GetType().Name, ["message"] = exception.Message }); }
        }
    }
}
