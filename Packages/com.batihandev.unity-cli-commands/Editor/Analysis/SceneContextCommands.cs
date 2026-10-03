using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;
using Exact = BatihanDev.UnityCliCommands.Editor.ExactObjectReference;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class SceneContextCommands
    {
        [CliCommand("analysis.scene-context", "Export scoped BFS objects, serialized values/references and field-type facts.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<SceneContextReport> Context([CliArg("maxDepth", "Inclusive root-relative depth.")] int maxDepth = 10, [CliArg("maxObjects", "Maximum exported objects.")] int maxObjects = 200, [CliArg("root", "Optional exact loaded subtree root.")] string root = null, [CliArg("includeValues", "Collect actual serialized JSON and labeled scalar values.")] bool includeValues = false, [CliArg("includeReferences", "Collect full serialized object-reference records.")] bool includeReferences = true, [CliArg("includeCodeDeps", "Collect reflected field-type relationships of exported scripts.")] bool includeCodeDeps = false) => ScenePerceptionFacts.Run("analysis.scene-context", () => SceneContextFacts.Collect(maxDepth, maxObjects, root, includeValues, includeReferences, includeCodeDeps));
        [CliCommand("analysis.scene-dependencies", "Report complete loaded serialized reference graph and exact subtree impact.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<SceneContextDependencies> Dependencies([CliArg("target", "Optional exact loaded subtree target.")] string target = null) => ScenePerceptionFacts.Run("analysis.scene-dependencies", () =>
        {
            var loaded = AnalysisSceneObjects.Loaded(true); var selected = target == null ? null : ScenePerceptionFacts.Resolve(target, loaded);
            var membership = new HashSet<string>(loaded.Where(go => selected == null || go == selected || go.transform.IsChildOf(selected.transform)).Select(Exact.ExactId), StringComparer.Ordinal);
            var missing = SceneAnalysisCommands.Missing(true, false, true, includeHidden:true); if (!missing.Ok) throw new ScenePerceptionRefusal(missing.Error.Code, missing.Error.Message);
            var missingKeys = new HashSet<string>(missing.Result.Issues.Select(issue => issue.Target + "|" + issue.ComponentIndex + "|" + issue.Property), StringComparer.Ordinal);
            var edges = SceneContextFacts.ReferenceFacts(loaded, missingKeys).Where(edge => !edge.Null && !edge.Missing && edge.Target != null).ToList();
            var incoming = edges.Where(edge => !membership.Contains(edge.SourceObject) && membership.Contains(edge.TargetObject ?? "")).ToList();
            var outgoing = edges.Where(edge => membership.Contains(edge.SourceObject) && !membership.Contains(edge.TargetObject ?? "")).ToList();
            var internalEdges = edges.Where(edge => membership.Contains(edge.SourceObject) && membership.Contains(edge.TargetObject ?? "")).ToList();
            var boundary = incoming.Concat(outgoing).ToList();
            return new SceneContextDependencies { Target = Exact.ExactId(selected), ObjectsAnalyzed = loaded.Count, TotalReferences = edges.Count, Edges = edges, Incoming = incoming, Outgoing = outgoing, Internal = internalEdges, Boundary = boundary,
                Scope = "Actual serialized reference properties across ordinary loaded scenes including hidden objects; component-target membership uses owning GameObject. Dynamic/runtime dependencies and complete delete effects are unknown.",
                Markdown = "# Serialized Dependency Report: " + SceneContextFacts.Escape(SceneManager.GetActiveScene().name) + "\n\n" + "Objects analyzed: " + loaded.Count + "; references: " + edges.Count + "; incoming: " + incoming.Count + "; outgoing: " + outgoing.Count + "; internal: " + internalEdges.Count + ".\n\n" + SceneContextFacts.ReferenceMarkdown(edges) };
        });
        [CliCommand("analysis.scene-report", "Return populated scene Markdown inline without publishing or importing files.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<SceneContextMarkdown> Report([CliArg("maxDepth", "Inclusive root-relative depth.")] int maxDepth = 10, [CliArg("maxObjects", "Maximum exported objects.")] int maxObjects = 500, [CliArg("root", "Optional exact loaded subtree root.")] string root = null) => ScenePerceptionFacts.Run("analysis.scene-report", () =>
        {
            var context = SceneContextFacts.Collect(maxDepth, maxObjects, root, true, true, true); var scripts = context.Objects.SelectMany(go => go.Components).Where(component => component.SourceClassification == "ProjectOwned").ToList(); var unknown = context.Objects.SelectMany(go => go.Components).Count(component => component.SourceClassification == "Unknown");
            var edges = context.References.Where(edge => !edge.Null && !edge.Missing).ToList(); var sb = new StringBuilder();
            sb.AppendLine("# Scene Report: " + SceneContextFacts.Escape(context.SceneName)); sb.AppendLine(); sb.AppendLine("Scope: " + context.Scope); sb.AppendLine("Loaded objects: " + context.TotalObjects + "; scope: " + context.ScopeObjects + "; exported: " + context.ExportedObjects + "; depth omitted: " + context.DepthOmitted + "; cap omitted: " + context.CountOmitted + ".");
            sb.AppendLine("Attached project-owned user scripts: " + scripts.Count + "; unknown source scripts: " + unknown + ". Source ownership uses actual MonoScript mapping under Assets or physically embedded project Packages.");
            sb.AppendLine(); sb.AppendLine("## Hierarchy"); sb.AppendLine();
            foreach (var go in context.Objects) sb.AppendLine(new string(' ', go.Depth * 2) + "- " + SceneContextFacts.Escape(go.Name) + " [" + string.Join(", ", go.Components.Where(component => component.Type != typeof(UnityEngine.Transform).FullName).Select(component => SceneContextFacts.Escape(component.Type) + (component.SourceClassification == "ProjectOwned" ? "*" : ""))) + "] (" + go.Target + ", scene " + SceneContextFacts.Escape(go.ScenePath) + ")");
            sb.AppendLine(); sb.AppendLine("## Script Fields"); sb.AppendLine(); sb.AppendLine("Serialized scalar records; unsupported kinds remain in component SerializedJson. No property getters were invoked.");
            foreach (var go in context.Objects)
                foreach (var component in go.Components.Where(component => component.SourceClassification == "ProjectOwned" || component.SourceClassification == "Unknown" || component.Missing))
                {
                    sb.AppendLine(); sb.AppendLine("### " + SceneContextFacts.Escape(go.Path + " / " + component.Type)); sb.AppendLine("Source: " + SceneContextFacts.Escape(component.SourcePath ?? "unmapped") + "; assembly: " + SceneContextFacts.Escape(component.Assembly) + "; classification: " + component.SourceClassification + ".");
                    if (component.Missing) { sb.AppendLine("Missing component."); continue; }
                    sb.AppendLine("| Field | Kind | Value |\n| --- | --- | --- |");
                    foreach (var field in component.Values) sb.AppendLine("| " + SceneContextFacts.Escape(field.Path) + " | " + field.Kind + " | " + (field.Supported ? SceneContextFacts.Escape(field.Value ?? "null") : "unsupported scalar; see serialized JSON") + " |");
                    sb.AppendLine("\nSerialized JSON:\n\n    " + component.SerializedJson);
                }
            sb.AppendLine(); sb.AppendLine("## Dependency Graph"); sb.AppendLine(); sb.AppendLine("Serialized object references from exported objects, including external targets:"); sb.AppendLine(SceneContextFacts.ReferenceMarkdown(edges));
            sb.AppendLine("Reflected field-type relationships; these are not concrete object references or runtime calls:\n\n| From | Field | To |\n| --- | --- | --- |");
            foreach (var edge in context.CodeDependencies) sb.AppendLine("| " + SceneContextFacts.Escape(edge.From) + " | " + SceneContextFacts.Escape(edge.DeclaringType + "." + edge.Field) + " | " + SceneContextFacts.Escape(edge.To) + " |");
            return new SceneContextMarkdown { Markdown = sb.ToString(), Context = context, UserScriptCount = scripts.Count, UnknownScriptCount = unknown, ReferenceCount = edges.Count, CodeReferenceCount = context.CodeDependencies.Count };
        });
        [CliCommand("analysis.scene-diff", "Capture or compare caller-held versioned same-Editor snapshots.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<SceneContextDiff> Diff([CliArg("snapshotJson", "Complete previous SnapshotJson; null captures.")] string snapshotJson = null) => ScenePerceptionFacts.Run("analysis.scene-diff", () =>
        {
            var current = Capture(); var result = new SceneContextDiff { Mode = snapshotJson == null ? "snapshot" : "diff", Snapshot = current, SnapshotJson = JsonConvert.SerializeObject(current) };
            if (snapshotJson == null) return result;
            var previous = Parse(snapshotJson);
            if (previous.EditorSession != current.EditorSession) throw new ScenePerceptionRefusal("ANALYSIS_SNAPSHOT_SESSION", "Compare a snapshot captured in this Editor process.");
            var old = previous.Objects.ToDictionary(row => row.Target, StringComparer.Ordinal); var now = current.Objects.ToDictionary(row => row.Target, StringComparer.Ordinal);
            result.Added = current.Objects.Where(row => !old.ContainsKey(row.Target)).ToList(); result.Removed = previous.Objects.Where(row => !now.ContainsKey(row.Target)).ToList();
            foreach (var row in current.Objects)
            {
                if (!old.TryGetValue(row.Target, out var before)) continue; var changes = new List<string>();
                if (row.Name != before.Name) changes.Add("name"); if (row.Path != before.Path || row.ScenePath != before.ScenePath) changes.Add("path"); if (!row.Components.SequenceEqual(before.Components)) changes.Add("components");
                if (Differs(row.Position, before.Position)) changes.Add("position"); if (Differs(row.Rotation, before.Rotation)) changes.Add("rotation"); if (Differs(row.Scale, before.Scale)) changes.Add("scale");
                if (changes.Count > 0) result.Modified.Add(new SceneContextModified { Target = row.Target, Name = row.Name, Path = row.Path, Changes = changes });
            }
            return result;
        });
        private static string Session()
        {
            using (var process = Process.GetCurrentProcess()) return process.Id.ToString(CultureInfo.InvariantCulture) + ":" + process.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture);
        }
        private static SceneContextSnapshot Capture() => new SceneContextSnapshot { Version = 1, EditorSession = Session(), Objects = AnalysisSceneObjects.Loaded(true).Select(go => new SceneContextSnapshotEntry { Target = Exact.ExactId(go), Name = go.name, Path = AnalysisSceneObjects.Path(go), ScenePath = go.scene.path, Components = go.GetComponents<UnityEngine.Component>().Select(component => component == null ? "<missing>" : component.GetType().FullName).ToArray(), Position = Vector(go.transform.position), Rotation = Vector(go.transform.eulerAngles), Scale = Vector(go.transform.localScale) }).ToList() };
        private static float[] Vector(Vector3 vector)
        {
            var values = new[] { vector.x, vector.y, vector.z }; if (values.Any(value => !ScenePerceptionFacts.Finite(value))) throw new ScenePerceptionRefusal("ANALYSIS_SNAPSHOT_INVALID", "Snapshot coordinates must be finite."); return values;
        }
        private static bool Differs(float[] a, float[] b) => Enumerable.Range(0, 3).Any(index => Math.Abs(a[index] - b[index]) > 0.0001f);
        private static SceneContextSnapshot Parse(string json)
        {
            try
            {
                var token = SceneContextJson.Parse(json);
                if (!(token is JObject envelope) || envelope["Version"]?.Type != JTokenType.Integer || (int)envelope["Version"] != 1 || envelope["EditorSession"]?.Type != JTokenType.String || string.IsNullOrEmpty((string)envelope["EditorSession"]) || !(envelope["Objects"] is JArray objects)) throw new FormatException();
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var row in objects)
                {
                    if (!(row is JObject entry) || entry["Target"]?.Type != JTokenType.String || !ulong.TryParse((string)entry["Target"], NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id == 0 || (string)entry["Target"] != id.ToString(CultureInfo.InvariantCulture) || !ids.Add(id.ToString(CultureInfo.InvariantCulture))) throw new FormatException();
                    foreach (var name in new[] { "Name", "Path", "ScenePath" }) if (entry[name]?.Type != JTokenType.String) throw new FormatException();
                    if (!(entry["Components"] is JArray components) || components.Count == 0 || components.Any(value => value.Type != JTokenType.String || string.IsNullOrEmpty((string)value))) throw new FormatException();
                    foreach (var name in new[] { "Position", "Rotation", "Scale" }) if (!(entry[name] is JArray vector) || vector.Count != 3 || vector.Any(value => (value.Type != JTokenType.Float && value.Type != JTokenType.Integer) || !ScenePerceptionFacts.Finite((float)value))) throw new FormatException();
                }
                return envelope.ToObject<SceneContextSnapshot>();
            }
            catch (Exception exception) when (exception is JsonException || exception is FormatException || exception is OverflowException || exception is InvalidCastException || exception is ArgumentException) { throw new ScenePerceptionRefusal("ANALYSIS_SNAPSHOT_INVALID", "Supply a complete supported snapshot with unique nonzero exact IDs and finite coordinates."); }
        }
    }
}
