using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class ValidationDiagnosticsCommands
    {
        [CliCommand("validation.scene", "Validate ordinary active loaded scene objects with explicit check toggles.", Tags = new[] { "unity-cli-commands", "validation" })]
        public static CommandResult<ScenePerceptionFindings> Scene([CliArg("checkMissingScripts", "Report missing component slots.")] bool checkMissingScripts = true, [CliArg("checkMissingPrefabs", "Report missing prefab source objects.")] bool checkMissingPrefabs = true, [CliArg("checkDuplicateNames", "Report exact-case loaded name clusters.")] bool checkDuplicateNames = true, [CliArg("checkEmptyGameObjects", "Report transform-only empty leaves.")] bool checkEmptyGameObjects = false) => ScenePerceptionFacts.Run("validation.scene", () =>
        {
            var objects = AnalysisSceneObjects.NativeGameObjects(); var findings = new System.Collections.Generic.List<ScenePerceptionFinding>();
            if (checkMissingScripts)
            {
                var missing = SceneAnalysisCommands.Missing(false, true, false); if (!missing.Ok) throw new ScenePerceptionRefusal(missing.Error.Code, missing.Error.Message);
                findings.AddRange(missing.Result.Issues.Select(issue => new ScenePerceptionFinding { Kind = "MissingScript", Severity = "Error", Target = issue.Target, Path = issue.Path, ScenePath = issue.ScenePath, Message = "Unresolved script at component index " + issue.ComponentIndex, Count = 1 }));
            }
            if (checkMissingPrefabs) findings.AddRange(objects.Where(PrefabUtility.IsPrefabAssetMissing).Select(go => ScenePerceptionFacts.Finding("MissingPrefab", "Warning", "Prefab source asset is missing.", go)));
            if (checkDuplicateNames) findings.AddRange(objects.GroupBy(go => go.name, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => ScenePerceptionFacts.Finding("DuplicateName", "Info", "Loaded objects share this exact name.", name:group.Key, count:group.Count())));
            if (checkEmptyGameObjects) findings.AddRange(objects.Where(go => go.transform.childCount == 0 && go.GetComponents<UnityEngine.Component>().Length == 1).Select(go => ScenePerceptionFacts.Finding("EmptyGameObject", "Info", "Transform-only empty leaf.", go)));
            return ScenePerceptionFacts.Report(findings, int.MaxValue);
        });
        [CliCommand("validation.mesh-colliders", "Report active loaded nonconvex MeshColliders and actual vertex counts.", Tags = new[] { "unity-cli-commands", "validation" })]
        public static CommandResult<ValidationDiagnosticsColliders> MeshColliders([CliArg("limit", "Maximum shown colliders.")] int limit = 50) => ScenePerceptionFacts.Run("validation.mesh-colliders", () =>
        {
            ScenePerceptionFacts.Bounds(limit); var rows = AnalysisSceneObjects.NativeComponents<MeshCollider>().Where(collider => !collider.convex).Select(collider => new ValidationDiagnosticsCollider { Target = Exact.ExactId(collider.gameObject), Path = AnalysisSceneObjects.Path(collider.gameObject), ScenePath = collider.gameObject.scene.path, VertexCount = collider.sharedMesh == null ? 0 : collider.sharedMesh.vertexCount }).ToList();
            return new ValidationDiagnosticsColliders { Total = rows.Count, Truncated = rows.Count > limit, Colliders = rows.Take(limit).ToList() };
        });
        [CliCommand("validation.shader-messages", "Report all actual shader compilation messages with separate error/warning counts.", Tags = new[] { "unity-cli-commands", "validation" })]
        public static CommandResult<ValidationDiagnosticsShaders> ShaderMessages([CliArg("limit", "Maximum shown message-bearing shaders.")] int limit = 50) => ScenePerceptionFacts.Run("validation.shader-messages", () =>
        {
            ScenePerceptionFacts.Bounds(limit); var rows = new System.Collections.Generic.List<ValidationDiagnosticsShader>();
            foreach (var guid in AssetDatabase.FindAssets("t:Shader"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid); var shader = AssetDatabase.LoadAssetAtPath<Shader>(path); if (shader == null) continue;
                var messages = ShaderUtil.GetShaderMessages(shader); if (messages.Length == 0) continue;
                rows.Add(new ValidationDiagnosticsShader { Name = shader.name, Path = path, MessageCount = messages.Length, ErrorCount = messages.Count(message => message.severity.ToString() == "Error"), WarningCount = messages.Count(message => message.severity.ToString() == "Warning"), Messages = messages.Select(message => new ValidationDiagnosticsShaderMessage { Severity = message.severity.ToString(), Message = message.message, File = message.file, Line = message.line }).ToList() });
            }
            return new ValidationDiagnosticsShaders { Total = rows.Count, MessageCount = rows.Sum(row => row.MessageCount), ErrorCount = rows.Sum(row => row.ErrorCount), WarningCount = rows.Sum(row => row.WarningCount), Truncated = rows.Count > limit, Shaders = rows.Take(limit).ToList() };
        });
        [CliCommand("validation.texture-sizes", "Report imported Texture2D dimensions strictly above the threshold without changing importers.", Tags = new[] { "unity-cli-commands", "validation" })]
        public static CommandResult<ValidationDiagnosticsTextures> TextureSizes([CliArg("maxRecommendedSize", "Nonnegative actual-dimension threshold.")] int maxRecommendedSize = 2048, [CliArg("limit", "Maximum shown textures.")] int limit = 50) => ScenePerceptionFacts.Run("validation.texture-sizes", () =>
        {
            ScenePerceptionFacts.Bounds(maxRecommendedSize, limit); var rows = new System.Collections.Generic.List<ValidationDiagnosticsTexture>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid); var importer = AssetImporter.GetAtPath(path) as TextureImporter; if (importer == null) continue;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (texture == null || (texture.width <= maxRecommendedSize && texture.height <= maxRecommendedSize)) continue;
                rows.Add(new ValidationDiagnosticsTexture { Path = path, Name = texture.name, Width = texture.width, Height = texture.height, MaxTextureSize = importer.maxTextureSize, Format = texture.format.ToString(), Recommendation = "Consider reducing actual dimensions to " + maxRecommendedSize + "x" + maxRecommendedSize + " or smaller." });
            }
            return new ValidationDiagnosticsTextures { MaxRecommendedSize = maxRecommendedSize, Total = rows.Count, Truncated = rows.Count > limit, Textures = rows.Take(limit).ToList() };
        });
    }
}
