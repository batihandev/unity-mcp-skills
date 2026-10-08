using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class ScenePerceptionCommands
    {
        [CliCommand("analysis.scene-summary", "Inspect active scene identity and fresh loaded-scene component/facility metrics.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<ScenePerceptionSummary> Summary([CliArg("includeComponentStats", "Include component counts.")] bool includeComponentStats = true, [CliArg("topComponentsLimit", "Maximum shown top components.")] int topComponentsLimit = 10) =>
            ScenePerceptionFacts.Run("analysis.scene-summary", () =>
            {
                ScenePerceptionFacts.Bounds(topComponentsLimit); var facts = ScenePerceptionFacts.Collect(includeComponentStats); var scene = SceneManager.GetActiveScene();
                return new ScenePerceptionSummary { SceneName = scene.name, ScenePath = scene.path, IsDirty = scene.isDirty, Scope = "Metrics: ordinary loaded scenes including hidden objects including inactive; roots/identity: active scene.", Stats = facts.Metrics,
                    TopComponents = includeComponentStats ? facts.Components.Where(kv => kv.Key != "Transform").OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(topComponentsLimit).Select(kv => new ScenePerceptionCount { Name = kv.Key, Count = kv.Value }).ToList() : null };
            });
        [CliCommand("analysis.scene-hotspots", "Report ranked loaded hierarchy, duplicate-name and empty-leaf heuristics.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<ScenePerceptionFindings> Hotspots([CliArg("deepHierarchyThreshold", "Inclusive depth threshold.")] int deepHierarchyThreshold = 8, [CliArg("largeChildCountThreshold", "Inclusive child threshold.")] int largeChildCountThreshold = 25, [CliArg("maxResults", "Maximum shown findings.")] int maxResults = 20) =>
            ScenePerceptionFacts.Run("analysis.scene-hotspots", () => { ScenePerceptionFacts.Bounds(deepHierarchyThreshold, largeChildCountThreshold, maxResults); var report = ScenePerceptionFacts.Report(ScenePerceptionFacts.Hotspots(AnalysisSceneObjects.Loaded(true), deepHierarchyThreshold, largeChildCountThreshold), maxResults); report.DeepHierarchyThreshold = deepHierarchyThreshold; report.LargeChildCountThreshold = largeChildCountThreshold; return report; });
        [CliCommand("analysis.scene-health", "Inspect complete scene hygiene/facility facts before capping findings.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<ScenePerceptionFindings> Health([CliArg("issueLimit", "Maximum shown findings.")] int issueLimit = 100, [CliArg("deepHierarchyThreshold", "Inclusive depth threshold.")] int deepHierarchyThreshold = 8, [CliArg("largeChildCountThreshold", "Inclusive child threshold.")] int largeChildCountThreshold = 25) =>
            ScenePerceptionFacts.Run("analysis.scene-health", () =>
            {
                ScenePerceptionFacts.Bounds(issueLimit, deepHierarchyThreshold, largeChildCountThreshold); var facts = ScenePerceptionFacts.Collect(false); var findings = ScenePerceptionFacts.Facilities(facts.Metrics, true, true);
                var missing = SceneAnalysisCommands.Missing(includeHidden:true); if (!missing.Ok) throw new ScenePerceptionRefusal(missing.Error.Code, missing.Error.Message);
                findings.AddRange(missing.Result.Issues.Select(issue => new ScenePerceptionFinding { Kind = issue.Kind, Severity = "Error", Target = issue.Target, Path = issue.Path, ScenePath = issue.ScenePath, Message = issue.Kind + " at component " + issue.ComponentIndex + (issue.Property == null ? "" : ":" + issue.Property), Count = 1 }));
                var hotspots = ScenePerceptionFacts.Hotspots(facts.Objects, deepHierarchyThreshold, largeChildCountThreshold); findings.AddRange(hotspots.Where(x => x.Kind != "DuplicateNameCluster"));
                var report = ScenePerceptionFacts.Report(findings, issueLimit, hotspots); report.DeepHierarchyThreshold = deepHierarchyThreshold; report.LargeChildCountThreshold = largeChildCountThreshold; return report;
            });
        [CliCommand("analysis.scene-contract", "Validate root names, defined tags/layers and scene facilities.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<ScenePerceptionContract> Contract([CliArg("requiredRootsJson", "Optional JSON string array; defaults structural roots.")] string requiredRootsJson = null, [CliArg("requiredTagsJson", "Optional JSON string array.")] string requiredTagsJson = null, [CliArg("requiredLayersJson", "Optional JSON string array.")] string requiredLayersJson = null, [CliArg("requireEventSystemForUi", "Require EventSystem when UGUI exists.")] bool requireEventSystemForUi = true) =>
            ScenePerceptionFacts.Run("analysis.scene-contract", () =>
            {
                var roots = Strings(requiredRootsJson, new[] { "Systems", "Managers", "Gameplay", "UIRoot" }); var tags = Strings(requiredTagsJson, Array.Empty<string>()); var layers = Strings(requiredLayersJson, Array.Empty<string>());
                var facts = ScenePerceptionFacts.Collect(false); var names = SceneManager.GetActiveScene().GetRootGameObjects().Where(go => facts.Objects.Contains(go)).Select(go => go.name).ToArray();
                var findings = new List<ScenePerceptionFinding>();
                foreach (var name in roots.Where(name => !names.Contains(name, StringComparer.OrdinalIgnoreCase))) findings.Add(ScenePerceptionFacts.Finding("MissingRoot", "Warning", "Required active-scene root is absent.", name:name));
                findings.AddRange(ScenePerceptionFacts.Facilities(facts.Metrics, requireEventSystemForUi, false));
                foreach (var name in tags.Where(name => !InternalEditorUtility.tags.Contains(name, StringComparer.OrdinalIgnoreCase))) findings.Add(ScenePerceptionFacts.Finding("MissingTagDefinition", "Warning", "Required tag is undefined.", name:name));
                foreach (var name in layers.Where(name => !InternalEditorUtility.layers.Contains(name, StringComparer.OrdinalIgnoreCase))) findings.Add(ScenePerceptionFacts.Finding("MissingLayerDefinition", "Warning", "Required layer is undefined.", name:name));
                return new ScenePerceptionContract { CheckedRoots = roots, CheckedTags = tags, CheckedLayers = layers, Passed = findings.Count == 0, Findings = findings, Errors = findings.Count(x => x.Severity == "Error"), Warnings = findings.Count(x => x.Severity == "Warning"), Info = findings.Count(x => x.Severity == "Info") };
            });
        private static string[] Strings(string json, string[] defaults)
        {
            if (json == null) return defaults;
            try
            {
                var token = SceneContextJson.Parse(json);
                if (!(token is JArray array) || array.Any(x => x.Type != JTokenType.String)) throw new FormatException();
                return array.Select(x => (string)x).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            }
            catch (Exception exception) when (exception is Newtonsoft.Json.JsonException || exception is FormatException) { throw new ScenePerceptionRefusal("ANALYSIS_INPUT_INVALID", "Supply a valid JSON array containing only strings."); }
        }
        [CliCommand("analysis.scene-tag-layers", "Report loaded tag/layer usage and unused defined layers.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<ScenePerceptionTagLayers> TagLayers() => ScenePerceptionFacts.Run("analysis.scene-tag-layers", () =>
        {
            var objects = AnalysisSceneObjects.Loaded(true); var used = new HashSet<int>(objects.Select(go => go.layer));
            Func<IEnumerable<IGrouping<string, GameObject>>, List<ScenePerceptionCount>> counts = groups => groups.OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal).Select(group => new ScenePerceptionCount { Name = group.Key, Count = group.Count() }).ToList();
            return new ScenePerceptionTagLayers { TotalObjects = objects.Count, UntaggedCount = objects.Count(go => go.tag == "Untagged"), Tags = counts(objects.GroupBy(go => go.tag)), Layers = counts(objects.GroupBy(go => string.IsNullOrEmpty(LayerMask.LayerToName(go.layer)) ? "Layer " + go.layer : LayerMask.LayerToName(go.layer))), EmptyDefinedLayers = Enumerable.Range(0, 32).Where(index => !used.Contains(index) && !string.IsNullOrEmpty(LayerMask.LayerToName(index))).Select(LayerMask.LayerToName).ToList() };
        });
        [CliCommand("analysis.scene-spatial", "Find loaded transform-distance hits, nearest when total exceeds the cap.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<ScenePerceptionSpatial> Spatial([CliArg("x", "Center X.")] float x = 0, [CliArg("y", "Center Y.")] float y = 0, [CliArg("z", "Center Z.")] float z = 0, [CliArg("radius", "Finite nonnegative inclusive radius.")] float radius = 10, [CliArg("nearObject", "Optional exact loaded GameObject.")] string nearObject = null, [CliArg("componentFilter", "Optional unique Component type.")] string componentFilter = null, [CliArg("maxResults", "Maximum shown hits.")] int maxResults = 50) => ScenePerceptionFacts.Run("analysis.scene-spatial", () =>
        {
            ScenePerceptionFacts.Bounds(maxResults); if (!ScenePerceptionFacts.Finite(x) || !ScenePerceptionFacts.Finite(y) || !ScenePerceptionFacts.Finite(z) || !ScenePerceptionFacts.Finite(radius) || radius < 0) throw new ScenePerceptionRefusal("ANALYSIS_INPUT_INVALID", "Use finite coordinates and a nonnegative radius.");
            var objects = AnalysisSceneObjects.Loaded(true); var center = nearObject == null ? new Vector3(x, y, z) : ScenePerceptionFacts.Resolve(nearObject, objects).transform.position;
            if (!ScenePerceptionFacts.Finite(center.x) || !ScenePerceptionFacts.Finite(center.y) || !ScenePerceptionFacts.Finite(center.z)) throw new ScenePerceptionRefusal("ANALYSIS_INPUT_INVALID", "The selected center is nonfinite.");
            var type = componentFilter == null ? null : AnalysisSceneObjects.ResolveType(componentFilter, typeof(UnityEngine.Component));
            var hits = new List<ScenePerceptionSpatialHit>();
            foreach (var go in objects)
            {
                if (type != null && go.GetComponent(type) == null) continue;
                var position = go.transform.position; var dx = (double)position.x - center.x; var dy = (double)position.y - center.y; var dz = (double)position.z - center.z; var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (distance <= radius) hits.Add(new ScenePerceptionSpatialHit { Target = Exact.ExactId(go), Name = go.name, Path = AnalysisSceneObjects.Path(go), ScenePath = go.scene.path, Distance = (float)distance, Position = new[] { position.x, position.y, position.z } });
            }
            return new ScenePerceptionSpatial { Center = new[] { center.x, center.y, center.z }, Radius = radius, TotalFound = hits.Count, Truncated = hits.Count > maxResults, Results = hits.Count <= maxResults ? hits : hits.OrderBy(hit => hit.Distance).Take(maxResults).ToList() };
        });
        [CliCommand("analysis.scene-materials", "Group shared materials by shader with slot-occurrence usage counts.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<ScenePerceptionMaterials> Materials([CliArg("includeProperties", "Include shader property name/type metadata.")] bool includeProperties = false) => ScenePerceptionFacts.Run("analysis.scene-materials", () =>
        {
            var materials = new Dictionary<Material, ScenePerceptionMaterial>();
            foreach (var renderer in AnalysisSceneObjects.NativeComponents<Renderer>())
                foreach (var material in renderer.sharedMaterials.Where(material => material != null))
                {
                    if (!materials.TryGetValue(material, out var row))
                    {
                        row = new ScenePerceptionMaterial { Target = Exact.ExactId(material), Name = material.name, Path = AssetDatabase.GetAssetPath(material), RenderQueue = material.renderQueue, Properties = includeProperties ? Properties(material.shader) : null }; materials.Add(material, row);
                    }
                    row.UserCount++; if (row.Users.Count < 5) row.Users.Add(AnalysisSceneObjects.Path(renderer.gameObject));
                }
            var groups = materials.GroupBy(kv => kv.Key.shader == null ? "null" : kv.Key.shader.name, StringComparer.Ordinal).Select(group => new ScenePerceptionShaderGroup { Shader = group.Key, MaterialCount = group.Count(), Materials = group.Select(kv => kv.Value).ToList() }).OrderByDescending(group => group.MaterialCount).ThenBy(group => group.Shader, StringComparer.Ordinal).ToList();
            return new ScenePerceptionMaterials { TotalMaterials = materials.Count, TotalShaders = groups.Count, Shaders = groups };
        });
        private static List<ScenePerceptionShaderProperty> Properties(Shader shader) => shader == null ? new List<ScenePerceptionShaderProperty>() : Enumerable.Range(0, ShaderUtil.GetPropertyCount(shader)).Select(index => new ScenePerceptionShaderProperty { Name = ShaderUtil.GetPropertyName(shader, index), Type = ShaderUtil.GetPropertyType(shader, index).ToString() }).ToList();
        [CliCommand("analysis.scene-performance", "Report ranked static component heuristics without measuring GPU costs.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<ScenePerceptionPerformance> Performance() => ScenePerceptionFacts.Run("analysis.scene-performance", () =>
        {
            var hints = new List<ScenePerceptionHint>(); var lights = AnalysisSceneObjects.NativeComponents<Light>().Count(light => light.shadows != LightShadows.None);
            if (lights > 4) hints.Add(Hint(1, "Lighting", lights + " shadow-casting lights", "Reduce shadow lights to four or use baked lighting.", "lighting-camera.md"));
            var renderers = AnalysisSceneObjects.NativeComponents<Renderer>().ToArray(); var nonstatic = renderers.Count(renderer => !renderer.gameObject.isStatic);
            if (nonstatic > 100) hints.Add(Hint(2, "Batching", nonstatic + " nonstatic renderers", "Review static flags.", "optimization.md"));
            var high = AnalysisSceneObjects.NativeComponents<MeshFilter>().Count(filter => MeshTriangleMetrics.Count(filter.sharedMesh) > 10000 && filter.GetComponent<LODGroup>() == null);
            if (high > 0) hints.Add(Hint(2, "Geometry", high + " high-poly meshes without same-object LOD", "Author LOD groups.", "optimization.md"));
            var slots = renderers.SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).ToArray(); var duplicate = slots.Length - slots.Distinct().Count();
            if (duplicate > 10) hints.Add(Hint(3, "Materials", duplicate + " duplicate material-slot references", "Review material consolidation.", "optimization.md"));
            var particles = AnalysisSceneObjects.NativeComponents<ParticleSystem>().Count; if (particles > 20) hints.Add(Hint(3, "Particles", particles + " particle systems", "Review pooling and particle counts.", null));
            if (hints.Count == 0) hints.Add(Hint(0, "OK", "No obvious performance issues", "Scene passes these static checks.", null));
            return new ScenePerceptionPerformance { Note = "Static heuristic advice; no measured GPU cost or runtime performance guarantee.", Hints = hints };
        });
        private static ScenePerceptionHint Hint(int priority, string category, string issue, string suggestion, string guide) => new ScenePerceptionHint { Priority = priority, Category = category, Issue = issue, Suggestion = suggestion, FixGuide = guide };
    }
}
