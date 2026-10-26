using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.Analysis
{
    internal static class SceneContextFacts
    {
        internal static SceneContextReport Collect(int maxDepth, int maxObjects, string root, bool values, bool references, bool code)
        {
            ScenePerceptionFacts.Bounds(maxDepth, maxObjects); var loaded = AnalysisSceneObjects.Loaded(true);
            var roots = root == null ? loaded.Where(go => go.scene == SceneManager.GetActiveScene() && go.transform.parent == null).ToArray() : new[] { ScenePerceptionFacts.Resolve(root, loaded) };
            var children = loaded.Where(go => go.transform.parent != null).GroupBy(go => go.transform.parent.gameObject).ToDictionary(group => group.Key, group => group.OrderBy(go => go.transform.GetSiblingIndex()).ToArray());
            var scope = new List<(GameObject Go, int Depth)>(); var queue = new Queue<(GameObject Go, int Depth)>(); foreach (var go in roots) queue.Enqueue((go, 0));
            while (queue.Count > 0) { var row = queue.Dequeue(); scope.Add(row); if (children.TryGetValue(row.Go, out var descendants)) foreach (var child in descendants) queue.Enqueue((child, row.Depth + 1)); }
            var eligible = scope.Where(row => row.Depth <= maxDepth).ToList(); var exported = eligible.Take(maxObjects).ToList();
            var map = values || code ? ScriptAnalysisFacts.SourceMap(false) : new Dictionary<Type, string>();
            var missing = references ? SceneAnalysisCommands.Missing(true, false, true, includeHidden:true) : null;
            if (missing != null && !missing.Ok) throw new ScenePerceptionRefusal(missing.Error.Code, missing.Error.Message);
            var missingKeys = missing == null ? new HashSet<string>() : new HashSet<string>(missing.Result.Issues.Select(issue => issue.Target + "|" + issue.ComponentIndex + "|" + issue.Property), StringComparer.Ordinal);
            var objects = exported.Select(row => Object(row.Go, row.Depth, values, map)).ToList();
            var edges = references ? ReferenceFacts(exported.Select(row => row.Go), missingKeys) : null;
            List<ScriptAnalysisEdge> codeEdges = null;
            if (code)
            {
                var types = AnalysisSceneObjects.LoadedTypes(); var sources = exported.SelectMany(row => row.Go.GetComponents<MonoBehaviour>()).Where(component => component != null).Select(component => component.GetType()).Where(type => ScriptAnalysisFacts.Eligible(type, true)).Distinct().ToArray();
                codeEdges = ScriptAnalysisFacts.Edges(sources, types);
                foreach (var name in codeEdges.SelectMany(edge => new[] { edge.From, edge.To }).Distinct(StringComparer.Ordinal))
                    if (types.Count(type => type.FullName == name && ScriptAnalysisFacts.Eligible(type, true)) != 1) throw new ArgumentException("An exported script graph endpoint has an ambiguous qualified type identity.");
            }
            return new SceneContextReport { SceneName = SceneManager.GetActiveScene().name, Scope = root == null ? "Active scene roots BFS; reference/code sources are exported objects only." : "Exact loaded subtree BFS; reference/code sources are exported objects only.", TotalObjects = loaded.Count, ScopeObjects = scope.Count, ExportedObjects = objects.Count, DepthOmitted = scope.Count - eligible.Count, CountOmitted = eligible.Count - exported.Count, Truncated = objects.Count < scope.Count, Objects = objects, References = edges, CodeDependencies = codeEdges };
        }
        private static SceneContextObject Object(GameObject go, int depth, bool values, Dictionary<Type, string> sourceMap)
        {
            var result = new SceneContextObject { Target = Exact.ExactId(go), Name = go.name, Path = AnalysisSceneObjects.Path(go), ScenePath = go.scene.path, SceneHandle = go.scene.handle.ToString(), Depth = depth };
            foreach (var component in go.GetComponents<UnityEngine.Component>())
            {
                if (component == null) { result.Components.Add(new SceneContextComponent { Missing = true, Type = "<missing>" }); continue; }
                var type = component.GetType(); sourceMap.TryGetValue(type, out var path);
                var row = new SceneContextComponent { Target = Exact.ExactId(component), Type = type.FullName, SerializedJson = values ? EditorJsonUtility.ToJson(component) : null, Values = values ? ScalarFacts(component) : null, SourcePath = path, Assembly = type.Assembly.GetName().Name, SourceClassification = component is MonoBehaviour ? Classification(path) : "EngineComponent" };
                result.Components.Add(row);
            }
            return result;
        }
        private static string Classification(string source)
        {
            if (source == null) return "Unknown";
            if (source.StartsWith("Assets/", StringComparison.Ordinal)) return "ProjectOwned";
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(source);
            if (package != null && package.source == UnityEditor.PackageManager.PackageSource.Embedded && source.StartsWith("Packages/", StringComparison.Ordinal))
            {
                var project = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
                var relativePackage = source.Split('/').Take(2).Aggregate((left, right) => left + "/" + right);
                var physical = System.IO.Path.GetFullPath(System.IO.Path.Combine(project, relativePackage));
                if (Directory.Exists(physical) && string.Equals(System.IO.Path.GetFullPath(package.resolvedPath), physical, System.IO.Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) return "ProjectOwned";
            }
            return "External";
        }
        private static List<SceneContextScalar> ScalarFacts(UnityEngine.Component component)
        {
            var result = new List<SceneContextScalar>();
            using (var serialized = new SerializedObject(component))
            {
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    string value = null; var supported = true;
                    switch (property.propertyType)
                    {
                        case SerializedPropertyType.Integer: case SerializedPropertyType.ArraySize: case SerializedPropertyType.Character: case SerializedPropertyType.LayerMask: value = property.longValue.ToString(CultureInfo.InvariantCulture); break;
                        case SerializedPropertyType.Boolean: value = property.boolValue ? "true" : "false"; break;
                        case SerializedPropertyType.Float: value = property.doubleValue.ToString("R", CultureInfo.InvariantCulture); break;
                        case SerializedPropertyType.String: value = property.stringValue; break;
                        case SerializedPropertyType.Enum: value = property.enumValueIndex >= 0 && property.enumValueIndex < property.enumNames.Length ? property.enumNames[property.enumValueIndex] : property.intValue.ToString(CultureInfo.InvariantCulture); break;
                        case SerializedPropertyType.ObjectReference: value = Exact.ExactId(property.objectReferenceValue); break;
                        default: supported = false; break;
                    }
                    result.Add(new SceneContextScalar { Path = property.propertyPath, Kind = property.propertyType.ToString(), Value = value, Supported = supported });
                }
            }
            return result;
        }
        internal static List<SceneContextReference> ReferenceFacts(IEnumerable<GameObject> sources, HashSet<string> missingKeys)
        {
            var result = new List<SceneContextReference>();
            foreach (var go in sources)
            {
                var components = go.GetComponents<UnityEngine.Component>();
                for (var index = 0; index < components.Length; index++)
                {
                    var component = components[index]; if (component == null) continue;
                    using (var serialized = new SerializedObject(component))
                    {
                        var property = serialized.GetIterator();
                        while (property.Next(true))
                        {
                            if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                            var target = property.objectReferenceValue; var owner = target is GameObject targetGo ? targetGo : target is UnityEngine.Component targetComponent ? targetComponent.gameObject : null;
                            var asset = target == null ? null : AssetDatabase.GetAssetPath(target); var isMissing = missingKeys.Contains(Exact.ExactId(go) + "|" + index + "|" + property.propertyPath);
                            result.Add(new SceneContextReference { SourceObject = Exact.ExactId(go), SourceComponent = Exact.ExactId(component), SourceComponentType = component.GetType().FullName, SourceSceneHandle = go.scene.handle.ToString(), TargetSceneHandle = owner == null || EditorUtility.IsPersistent(owner) ? null : owner.scene.handle.ToString(), SourcePath = AnalysisSceneObjects.Path(go), SourceScenePath = go.scene.path, Property = property.propertyPath, Target = Exact.ExactId(target), TargetObject = Exact.ExactId(owner), TargetPath = owner == null ? null : AnalysisSceneObjects.Path(owner), TargetScenePath = owner == null || EditorUtility.IsPersistent(owner) ? null : owner.scene.path, TargetAssetPath = string.IsNullOrEmpty(asset) ? null : asset, TargetType = target == null ? null : target.GetType().FullName, Missing = isMissing, Null = target == null && !isMissing });
                        }
                    }
                }
            }
            return result;
        }
        internal static string Escape(string text) => (text ?? "").Replace("\\", "\\\\").Replace("|", "\\|").Replace("\r", "\\r").Replace("\n", "\\n").Replace("<", "&lt;").Replace(">", "&gt;").Replace("`", "\\`");
        internal static string ReferenceMarkdown(IEnumerable<SceneContextReference> edges)
        {
            var sb = new StringBuilder("| Source | Property | Target |\n| --- | --- | --- |\n");
            foreach (var edge in edges) sb.AppendLine("| " + Escape(edge.SourceScenePath + ":" + edge.SourcePath + " (" + edge.SourceComponent + ")") + " | " + Escape(edge.Property) + " | " + Escape(edge.TargetAssetPath ?? edge.TargetScenePath + ":" + edge.TargetPath + " (" + edge.Target + ")") + " |");
            return sb.ToString();
        }
    }
}
