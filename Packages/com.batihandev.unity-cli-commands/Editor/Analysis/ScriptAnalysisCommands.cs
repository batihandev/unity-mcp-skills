using System;
using System.Collections.Generic;
using System.Linq;
using BatihanDev.UnityCliCommands.Foundation;
using Unity.Pipeline.Commands;
namespace BatihanDev.UnityCliCommands.Analysis
{
    public static class ScriptAnalysisCommands
    {
        [CliCommand("analysis.script-inspect", "Inspect declared script member metadata without invoking getters.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<ScriptAnalysisInspection> Inspect([CliArg("scriptName", "Unique simple or exact qualified script name.", Required = true)] string scriptName, [CliArg("includePrivate", "Include declared private members.")] bool includePrivate = false) => ScenePerceptionFacts.Run("analysis.script-inspect", () => ScriptAnalysisFacts.Inspect(ScriptAnalysisFacts.Resolve(scriptName, false), includePrivate));
        [CliCommand("analysis.script-graph", "Inspect bidirectional field-type closure and labeled cyclic reading order.", Tags = new[] { "unity-cli-commands", "analysis" })]
        public static CommandResult<ScriptAnalysisGraph> Graph([CliArg("scriptName", "Unique simple or exact qualified eligible type.", Required = true)] string scriptName, [CliArg("maxHops", "Nonnegative incoming/outgoing BFS depth.")] int maxHops = 2, [CliArg("includeDetails", "Include declared field and callback metadata.")] bool includeDetails = true) => ScenePerceptionFacts.Run("analysis.script-graph", () =>
        {
            ScenePerceptionFacts.Bounds(maxHops); var entry = ScriptAnalysisFacts.Resolve(scriptName, true); var types = AnalysisSceneObjects.LoadedTypes().Where(type => ScriptAnalysisFacts.Eligible(type, true)).ToArray();
            var edges = ScriptAnalysisFacts.Edges(types, types); var visited = new Dictionary<string, int>(StringComparer.Ordinal) { [entry.FullName] = 0 }; var queue = new Queue<string>(); queue.Enqueue(entry.FullName);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue(); if (visited[node] >= maxHops) continue;
                foreach (var neighbor in edges.Where(edge => edge.From == node || edge.To == node).Select(edge => edge.From == node ? edge.To : edge.From).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal))
                    if (!visited.ContainsKey(neighbor)) { visited[neighbor] = visited[node] + 1; queue.Enqueue(neighbor); }
            }
            var reached = edges.Where(edge => visited.ContainsKey(edge.From) && visited.ContainsKey(edge.To)).ToList(); var map = ScriptAnalysisFacts.SourceMap(true);
            var nodes = new List<ScriptAnalysisNode>();
            foreach (var kv in visited.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal))
            {
                var matches = types.Where(type => type.FullName == kv.Key).ToArray(); if (matches.Length != 1) throw new ArgumentException("Reached type identity is ambiguous."); var type = matches[0];
                nodes.Add(new ScriptAnalysisNode { Name = type.Name, FullName = type.FullName, Assembly = type.Assembly.GetName().Name, Hop = kv.Value, Kind = ScriptAnalysisFacts.Kind(type), BaseClass = type.BaseType?.FullName, FilePath = map.TryGetValue(type, out var path) ? path : null,
                    DependsOn = reached.Where(edge => edge.From == kv.Key).Select(edge => edge.To).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToList(), DependedBy = reached.Where(edge => edge.To == kv.Key).Select(edge => edge.From).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToList(), Fields = includeDetails ? ScriptAnalysisFacts.DeclaredFields(type, false).Select(ScriptAnalysisFacts.Field).ToList() : null, UnityCallbacks = includeDetails ? ScriptAnalysisFacts.Lifecycle(type) : null });
            }
            var indegree = visited.Keys.ToDictionary(name => name, name => reached.Count(edge => edge.To == name), StringComparer.Ordinal); var ready = new SortedSet<string>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key), StringComparer.Ordinal); var order = new List<string>();
            while (ready.Count > 0) { var node = ready.Min; ready.Remove(node); order.Add(node); foreach (var edge in reached.Where(edge => edge.From == node)) if (--indegree[edge.To] == 0) ready.Add(edge.To); }
            var residual = visited.Keys.Except(order, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToList(); order.AddRange(residual);
            return new ScriptAnalysisGraph { EntryScript = entry.FullName, MaxHops = maxHops, TotalScriptsReached = nodes.Count, Scope = "Reflected field-type relationships; reading order follows dependent-to-dependency Kahn traversal. Cyclic remainder includes cycle-blocked nodes; runtime calls are unknown.", Scripts = nodes, Edges = reached, SuggestedReadOrder = order, CyclicRemainder = residual };
        });
    }
}
