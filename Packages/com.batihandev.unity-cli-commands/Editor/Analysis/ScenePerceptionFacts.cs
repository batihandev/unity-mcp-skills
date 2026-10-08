using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.Analysis
{
    internal sealed class ScenePerceptionCollection
    {
        internal IReadOnlyList<GameObject> Objects;
        internal ScenePerceptionMetrics Metrics;
        internal readonly Dictionary<string, int> Components = new Dictionary<string, int>(StringComparer.Ordinal);
    }
    internal static class ScenePerceptionFacts
    {
        internal static CommandResult<T> Run<T>(string route, Func<T> collect)
        {
            var schema = "unity." + route + "@1";
            var compatibility = CompatibilityPolicy.CheckInstalled();
            if (!compatibility.Ok) return CommandResult<T>.Failure(schema, compatibility.Error);
            try { return CommandResult<T>.Success(schema, collect()); }
            catch (ScenePerceptionRefusal refusal) { return CommandResult<T>.Failure(schema, refusal.Code, refusal.Message); }
            catch (ArgumentException exception) { return CommandResult<T>.Failure(schema, "ANALYSIS_TYPE_INVALID", exception.Message); }
            catch (Exception exception) when (ProjectPathPolicy.IsExpectedPathException(exception)) { return ProjectPathPolicy.FileSystemFailure<T>(schema, "Assets", exception); }
            catch (Exception exception) { return CommandResult<T>.Failure(schema, "ANALYSIS_READ_FAILED", "Inspection failed; review the error before retrying.", new Dictionary<string, object> { ["exceptionType"] = exception.GetType().Name, ["message"] = exception.Message }); }
        }
        internal static void Bounds(params int[] values)
        {
            if (values.Any(value => value < 0)) throw new ScenePerceptionRefusal("ANALYSIS_INPUT_INVALID", "Use nonnegative depths, thresholds, and limits.");
        }
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static int Depth(GameObject go) { var depth = 0; for (var p = go.transform.parent; p != null; p = p.parent) depth++; return depth; }
        internal static GameObject Resolve(string reference, IReadOnlyList<GameObject> scope)
        {
            var go = Exact.Resolve<GameObject>(reference);
            if (go == null || !scope.Contains(go)) throw new ScenePerceptionRefusal("ANALYSIS_TARGET_INVALID", "Resolve an exact ordinary loaded scene GameObject reference.");
            return go;
        }
        internal static ScenePerceptionCollection Collect(bool components = true)
        {
            var facts = new ScenePerceptionCollection { Objects = AnalysisSceneObjects.Loaded(true), Metrics = new ScenePerceptionMetrics() };
            var metrics = facts.Metrics;
            metrics.TotalObjects = facts.Objects.Count;
            metrics.RootObjects = SceneManager.GetActiveScene().GetRootGameObjects().Count(go => facts.Objects.Contains(go));
            foreach (var go in facts.Objects)
            {
                if (go.activeInHierarchy) metrics.ActiveObjects++; else metrics.InactiveObjects++;
                metrics.MaxHierarchyDepth = Math.Max(metrics.MaxHierarchyDepth, Depth(go));
                if (PrefabUtility.IsPartOfPrefabInstance(go) && !PrefabUtility.IsPartOfPrefabAsset(go)) metrics.PrefabInstances++;
                var attached = go.GetComponents<UnityEngine.Component>();
                if (attached.Length == 1 && go.transform.childCount == 0) metrics.EmptyLeafObjects++;
                foreach (var component in attached)
                {
                    if (component == null) continue;
                    var type = component.GetType(); var name = type.Name;
                    if (components) facts.Components[name] = facts.Components.TryGetValue(name, out var count) ? count + 1 : 1;
                    if (component is Camera) { metrics.Cameras++; if (go.CompareTag("MainCamera")) metrics.MainCameras++; }
                    if (component is Light) metrics.Lights++;
                    if (component is Canvas) metrics.Canvases++;
                    if (component is AudioListener) metrics.AudioListeners++;
                    if (IsType(type, "UnityEngine.EventSystems.EventSystem")) metrics.EventSystems++;
                    if (IsType(type, "UnityEngine.UI.Graphic")) metrics.HasUgui = true;
                    if (IsType(type, "UnityEngine.UIElements.UIDocument")) metrics.HasUiToolkit = true;
                }
            }
            metrics.HasUgui |= metrics.Canvases > 0;
            metrics.DisabledRatio = Math.Round(metrics.InactiveObjects / (double)Math.Max(1, metrics.TotalObjects), 3);
            return facts;
        }
        private static bool IsType(Type type, string fullName)
        {
            for (; type != null; type = type.BaseType) if (type.FullName == fullName) return true;
            return false;
        }
        internal static ScenePerceptionFinding Finding(string kind, string severity, string message, GameObject go = null, string name = null, int count = 0, int depth = 0) =>
            new ScenePerceptionFinding { Kind = kind, Severity = severity, Message = message, Name = go == null ? name : go.name,
                Target = Exact.ExactId(go), Path = go == null ? null : AnalysisSceneObjects.Path(go), ScenePath = go == null ? null : go.scene.path, Count = count, Depth = depth };
        internal static List<ScenePerceptionFinding> Facilities(ScenePerceptionMetrics metrics, bool eventSystem, bool audio)
        {
            var result = new List<ScenePerceptionFinding>();
            if (metrics.MainCameras == 0) result.Add(Finding("MissingMainCamera", "Error", "No MainCamera-tagged camera was found."));
            if (metrics.Lights == 0) result.Add(Finding("MissingLight", "Warning", "No Light component was found."));
            if (metrics.HasUgui && eventSystem && metrics.EventSystems == 0) result.Add(Finding("MissingEventSystem", "Error", "UGUI objects require an EventSystem."));
            if (metrics.HasUgui && metrics.Canvases == 0) result.Add(Finding("MissingCanvas", "Error", "UI graphics require a Canvas."));
            if (audio && metrics.Cameras > 0 && metrics.AudioListeners == 0) result.Add(Finding("MissingAudioListener", "Warning", "Cameras exist without an AudioListener."));
            return result;
        }
        internal static List<ScenePerceptionFinding> Hotspots(IReadOnlyList<GameObject> objects, int deep, int children)
        {
            var result = new List<ScenePerceptionFinding>();
            foreach (var go in objects)
            {
                var depth = Depth(go);
                if (depth >= deep) result.Add(Finding("DeepHierarchy", (long)depth >= (long)deep + 3 ? "Warning" : "Info", "Hierarchy depth reaches the configured threshold.", go, count:depth, depth:depth));
                if (go.transform.childCount >= children) result.Add(Finding("LargeChildSet", (long)go.transform.childCount >= (long)children * 2 ? "Warning" : "Info", "Direct children reach the configured threshold.", go, count:go.transform.childCount));
            }
            foreach (var group in objects.GroupBy(go => go.name, StringComparer.Ordinal).Where(group => group.Count() > 1))
                result.Add(Finding("DuplicateNameCluster", group.Count() >= 5 ? "Warning" : "Info", "Multiple loaded objects share this exact name.", name:group.Key, count:group.Count()));
            foreach (var group in objects.Where(go => go.transform.childCount == 0 && go.GetComponents<UnityEngine.Component>().Length == 1)
                .GroupBy(go => go.scene.handle.ToString() + ":" + (go.transform.parent == null ? "<root>" : Exact.ExactId(go.transform.parent.gameObject)), StringComparer.Ordinal).Where(group => group.Count() >= 3))
            {
                var go = group.First(); var parent = go.transform.parent;
                var finding = Finding("EmptyLeafCluster", "Info", "Transform-only empty leaves share this scoped parent.", parent == null ? null : parent.gameObject, count:group.Count());
                finding.ScenePath = go.scene.path; if (parent == null) { finding.Path = "<root>"; finding.Target = "scene:" + go.scene.handle; }
                result.Add(finding);
            }
            return result.OrderBy(finding => Rank(finding.Severity)).ThenByDescending(finding => finding.Count).ThenByDescending(finding => finding.Depth)
                .ThenBy(finding => finding.Kind, StringComparer.Ordinal).ThenBy(finding => finding.ScenePath, StringComparer.Ordinal).ThenBy(finding => finding.Path ?? finding.Name, StringComparer.Ordinal).ThenBy(finding => finding.Target, StringComparer.Ordinal).ToList();
        }
        internal static int Rank(string severity) => severity == "Error" ? 0 : severity == "Warning" ? 1 : 2;
        internal static ScenePerceptionFindings Report(List<ScenePerceptionFinding> findings, int limit, List<ScenePerceptionFinding> hotspots = null)
        {
            var unique = findings.GroupBy(f => string.Join("|", f.Kind, f.Severity, f.Target, f.ScenePath, f.Path ?? f.Name, f.Message), StringComparer.Ordinal).Select(group => group.First()).ToList();
            var shown = unique.Take(limit).ToList();
            return new ScenePerceptionFindings { SceneName = SceneManager.GetActiveScene().name, Total = unique.Count, Truncated = unique.Count > shown.Count,
                Findings = shown, Hotspots = hotspots == null ? new List<ScenePerceptionFinding>() : hotspots.Take(limit).ToList(), HotspotTotal = hotspots == null ? 0 : hotspots.Count, HotspotsTruncated = hotspots != null && hotspots.Count > limit, Errors = unique.Count(x => x.Severity == "Error"), Warnings = unique.Count(x => x.Severity == "Warning"), Info = unique.Count(x => x.Severity == "Info"),
                ShownErrors = shown.Count(x => x.Severity == "Error"), ShownWarnings = shown.Count(x => x.Severity == "Warning"), ShownInfo = shown.Count(x => x.Severity == "Info"),
                SuggestedGuides = shown.Select(Guide).Where(value => value != null).Distinct(StringComparer.Ordinal).ToList() };
        }
        private static string Guide(ScenePerceptionFinding finding)
        {
            switch (finding.Kind)
            {
                case "MissingReference": case "MissingScript": return "validation.md";
                case "MissingMainCamera": case "MissingLight": return "lighting-camera.md";
                case "MissingEventSystem": case "MissingCanvas": return "ui.md";
                case "MissingAudioListener": return "component.md";
                default: return "perception.md";
            }
        }
    }
    internal sealed class ScenePerceptionRefusal : Exception
    {
        internal string Code { get; }
        internal ScenePerceptionRefusal(string code, string message) : base(message) { Code = code; }
    }
}
