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
    public static class SceneAnalysisCommands
    {
        [CliCommand("analysis.scene-missing", "Report unresolved scripts and serialized references in loaded scenes and optional prefab assets.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<SceneMissingReport> Missing(
            [CliArg("includeInactive", "Include inactive objects.")] bool includeInactive = true,
            [CliArg("includeScripts", "Report unresolved scripts.")] bool includeScripts = true,
            [CliArg("includeReferences", "Report unresolved object references.")] bool includeReferences = true,
            [CliArg("firstReferencePerComponent", "Report only the first missing reference per component.")] bool firstReferencePerComponent = false,
            [CliArg("visibleReferencesOnly", "Inspect only Inspector-visible serialized properties.")] bool visibleReferencesOnly = false,
            [CliArg("limit", "Maximum shown issues; -1 is uncapped.")] int limit = -1,
            [CliArg("searchInPrefabs", "Also inspect prefab asset contents without mutation.")] bool searchInPrefabs = false,
            [CliArg("includeHidden", "Include hidden hierarchy objects when includeInactive=true.")] bool includeHidden = false)
        {
            const string schema = "unity.analysis.scene-missing@1";
            var compatibility = CompatibilityPolicy.CheckInstalled(); if (!compatibility.Ok) return CommandResult<SceneMissingReport>.Failure(schema, compatibility.Error);
            if (limit < -1) return CommandResult<SceneMissingReport>.Failure(schema, "ANALYSIS_INPUT_INVALID", "Use -1 for uncapped results or a nonnegative limit.");
            try
            {
                var issues = new List<SceneIssue>();
                foreach (var go in (includeInactive ? AnalysisSceneObjects.Loaded(true, !includeHidden) : AnalysisSceneObjects.NativeGameObjects())) Inspect(go, includeScripts, includeReferences, firstReferencePerComponent, visibleReferencesOnly, null, issues);
                if (searchInPrefabs)
                {
                    var scan = AssetAnalysisFacts.Scan("Assets"); if (!scan.Ok) return CommandResult<SceneMissingReport>.Failure(schema, scan.Error);
                    foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid); GameObject contents = null;
                        try
                        {
                            contents = PrefabUtility.LoadPrefabContents(path);
                            foreach (var transform in contents.GetComponentsInChildren<UnityEngine.Transform>(true))
                                Inspect(transform.gameObject, includeScripts, includeReferences, firstReferencePerComponent, visibleReferencesOnly, path, issues);
                        }
                        finally { if (contents != null) PrefabUtility.UnloadPrefabContents(contents); }
                    }
                }
                return CommandResult<SceneMissingReport>.Success(schema, new SceneMissingReport { Total = issues.Count, Truncated = limit >= 0 && issues.Count > limit,
                    MissingScripts = issues.Count(issue => issue.Kind == "MissingScript"), MissingReferences = issues.Count(issue => issue.Kind == "MissingReference"),
                    Issues = limit < 0 ? issues : issues.Take(limit).ToList() });
            }
            catch (Exception exception) { return ReadFailure<SceneMissingReport>(schema, exception); }
        }
        private static void Inspect(GameObject go, bool scripts, bool references, bool first, bool visible, string prefab, List<SceneIssue> issues)
        {
            var components = go.GetComponents<UnityEngine.Component>();
            for (var index = 0; index < components.Length; index++)
            {
                var component = components[index];
                if (component == null)
                {
                    if (scripts) issues.Add(new SceneIssue { Target = Exact.ExactId(go), Path = AnalysisSceneObjects.Path(go), Kind = "MissingScript", ComponentIndex = index, ScenePath = prefab == null ? go.scene.path : null, PrefabPath = prefab });
                    continue;
                }
                if (!references) continue;
                using (var serialized = new SerializedObject(component))
                {
                    var property = serialized.GetIterator();
                    while (visible ? property.NextVisible(true) : property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != null || UnityEngine.EntityId.ToULong(property.objectReferenceEntityIdValue) == 0) continue;
                        issues.Add(new SceneIssue { Target = Exact.ExactId(go), Path = AnalysisSceneObjects.Path(go), Kind = "MissingReference", Component = component.GetType().FullName,
                            ComponentIndex = index, Property = property.propertyPath, ScenePath = prefab == null ? go.scene.path : null, PrefabPath = prefab });
                        if (first) break;
                    }
                }
            }
        }
        [CliCommand("analysis.scene-rendering", "Report active Renderer and MeshFilter totals with strict heuristic thresholds.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<SceneRenderingReport> Rendering([CliArg("polyThreshold", "Strict triangle threshold.")] int polyThreshold = 10000,
            [CliArg("materialThreshold", "Strict material-slot threshold.")] int materialThreshold = 5)
        {
            const string schema = "unity.analysis.scene-rendering@1";
            var compatibility = CompatibilityPolicy.CheckInstalled(); if (!compatibility.Ok) return CommandResult<SceneRenderingReport>.Failure(schema, compatibility.Error);
            if (polyThreshold < 0 || materialThreshold < 0) return CommandResult<SceneRenderingReport>.Failure(schema, "ANALYSIS_INPUT_INVALID", "Use nonnegative rendering thresholds.");
            try
            {
                var report = new SceneRenderingReport();
                foreach (var renderer in AnalysisSceneObjects.NativeComponents<Renderer>())
                {
                    report.TotalRenderers++; var filter = renderer.GetComponent<MeshFilter>();
                    var triangles = MeshTriangleMetrics.Count(filter == null ? null : filter.sharedMesh);
                    var slots = renderer.sharedMaterials.Length; report.TotalTriangles = checked(report.TotalTriangles + triangles); report.TotalMaterialSlots += slots;
                    if (triangles > polyThreshold) report.Issues.Add(new SceneIssue { Target = Exact.ExactId(renderer.gameObject), Path = AnalysisSceneObjects.Path(renderer.gameObject), Kind = "HighPoly", Triangles = triangles });
                    if (slots > materialThreshold) report.Issues.Add(new SceneIssue { Target = Exact.ExactId(renderer.gameObject), Path = AnalysisSceneObjects.Path(renderer.gameObject), Kind = "ExcessiveMaterials", MaterialCount = slots });
                }
                return CommandResult<SceneRenderingReport>.Success(schema, report);
            }
            catch (Exception exception) { return ReadFailure<SceneRenderingReport>(schema, exception); }
        }
        [CliCommand("analysis.scene-transparent", "Report first qualifying material per active Renderer as an overdraw heuristic.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<SceneTransparentReport> Transparent([CliArg("limit", "Maximum shown renderer candidates.")] int limit = 50)
        {
            const string schema = "unity.analysis.scene-transparent@1";
            var compatibility = CompatibilityPolicy.CheckInstalled(); if (!compatibility.Ok) return CommandResult<SceneTransparentReport>.Failure(schema, compatibility.Error);
            if (limit < 0) return CommandResult<SceneTransparentReport>.Failure(schema, "ANALYSIS_INPUT_INVALID", "Use a nonnegative candidate limit.");
            try
            {
                var rows = new List<SceneIssue>();
                foreach (var renderer in AnalysisSceneObjects.NativeComponents<Renderer>())
                {
                    var material = renderer.sharedMaterials.FirstOrDefault(value => value != null && value.renderQueue >= 2500);
                    if (material == null) continue;
                    rows.Add(new SceneIssue { Target = Exact.ExactId(renderer.gameObject), Path = AnalysisSceneObjects.Path(renderer.gameObject), Material = Exact.ExactId(material), Shader = material.shader == null ? null : material.shader.name, RenderQueue = material.renderQueue, Kind = "TransparentCandidate" });
                }
                return CommandResult<SceneTransparentReport>.Success(schema, new SceneTransparentReport { Objects = rows.Take(limit).ToList(), Total = rows.Count, Truncated = rows.Count > limit,
                    Note = "Render queue is a heuristic; this report does not measure GPU overdraw." });
            }
            catch (Exception exception) { return ReadFailure<SceneTransparentReport>(schema, exception); }
        }
        private static CommandResult<T> ReadFailure<T>(string schema, Exception exception) => CommandResult<T>.Failure(schema, "ANALYSIS_READ_FAILED", "Scene inspection failed; inspect the error before retrying.",
            new Dictionary<string, object> { ["exceptionType"] = exception.GetType().Name, ["message"] = exception.Message });
    }
}
