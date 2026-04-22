# script_dependency_graph

**Skill:** `script_dependency_graph`
**C# method:** `PerceptionSkills.ScriptDependencyGraph`

## Signature

```
ScriptDependencyGraph(
    string scriptName,
    int maxHops = 2,
    bool includeDetails = true)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `scriptName` | `string` | required | Entry class name (case-insensitive) |
| `maxHops` | `int` | `2` | BFS depth in both dependency directions |
| `includeDetails` | `bool` | `true` | Whether to include fields and Unity callbacks per script |

## Return Shape

Returns `success`, `entryScript`, `totalScriptsReached`, `maxHops`, `scripts` array (name, hop, kind, baseClass, filePath, dependsOn, dependedBy, fields, unityCallbacks), `edges` array (from, to, type, detail), `suggestedReadOrder`.

**Prerequisites:** [`skills_common`](../_shared/skills_common.md)

## RunCommand Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string scriptName = "MyScript"; // replace with target class name
        int maxHops = 2;
        bool includeDetails = true;

        if (string.IsNullOrEmpty(scriptName))
        {
            result.SetValue(new { success = false, error = "scriptName is required" });
            return;
        }

        var allTypes = SkillsCommon.GetAllLoadedTypes()
            .Where(t => t.IsClass && IsUserScript(t))
            .ToList();

        var entryType = allTypes.FirstOrDefault(t => t.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase));
        if (entryType == null)
        {
            result.SetValue(new { success = false, error = $"Script '{scriptName}' not found among user scripts" });
            return;
        }

        var entryName = entryType.Name;
        var codeEdges = CollectCodeDependencies();

        var outgoing = new Dictionary<string, HashSet<string>>();
        var incoming = new Dictionary<string, HashSet<string>>();
        foreach (var e in codeEdges)
        {
            if (!outgoing.ContainsKey(e.fromObject)) outgoing[e.fromObject] = new HashSet<string>();
            outgoing[e.fromObject].Add(e.toObject);
            if (!incoming.ContainsKey(e.toObject)) incoming[e.toObject] = new HashSet<string>();
            incoming[e.toObject].Add(e.fromObject);
        }

        var visited = new Dictionary<string, int>();
        var queue = new Queue<(string name, int hop)>();
        visited[entryName] = 0;
        queue.Enqueue((entryName, 0));

        while (queue.Count > 0)
        {
            var (current, hop) = queue.Dequeue();
            if (hop >= maxHops) continue;

            if (outgoing.TryGetValue(current, out var outs))
                foreach (var n in outs) if (!visited.ContainsKey(n)) { visited[n] = hop + 1; queue.Enqueue((n, hop + 1)); }

            if (incoming.TryGetValue(current, out var ins))
                foreach (var n in ins) if (!visited.ContainsKey(n)) { visited[n] = hop + 1; queue.Enqueue((n, hop + 1)); }
        }

        // Build file path lookup
        var filePathMap = new Dictionary<string, string>();
        foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (ms == null) continue;
            var cls = ms.GetClass();
            if (cls != null && visited.ContainsKey(cls.Name))
                filePathMap[cls.Name] = path;
        }

        var typeMap = new Dictionary<string, Type>();
        foreach (var t in allTypes)
            if (visited.ContainsKey(t.Name) && !typeMap.ContainsKey(t.Name))
                typeMap[t.Name] = t;

        var scripts = new List<object>();
        foreach (var kv in visited.OrderBy(k => k.Value).ThenBy(k => k.Key))
        {
            var sName = kv.Key;
            var hop = kv.Value;
            var type = typeMap.ContainsKey(sName) ? typeMap[sName] : null;

            var dependsOn = outgoing.ContainsKey(sName) ? outgoing[sName].Where(visited.ContainsKey).OrderBy(n => n).ToList() : new List<string>();
            var dependedBy = incoming.ContainsKey(sName) ? incoming[sName].Where(visited.ContainsKey).OrderBy(n => n).ToList() : new List<string>();

            string kind = null, baseClass = null;
            List<object> fields = null;
            List<string> callbacks = null;

            if (type != null)
            {
                kind = typeof(MonoBehaviour).IsAssignableFrom(type) ? "MonoBehaviour"
                    : typeof(ScriptableObject).IsAssignableFrom(type) ? "ScriptableObject"
                    : "Class";
                baseClass = type.BaseType?.Name;

                if (includeDetails)
                {
                    fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                        .Where(f => !f.Name.StartsWith("<"))
                        .Select(f => (object)new { name = f.Name, type = GetFriendlyTypeName(f.FieldType), serializable = f.IsPublic || f.GetCustomAttribute<SerializeField>() != null })
                        .ToList();

                    if (typeof(MonoBehaviour).IsAssignableFrom(type))
                        callbacks = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                            .Where(m => UnityCallbacks.Contains(m.Name))
                            .Select(m => m.Name).ToList();
                }
            }

            scripts.Add(new { name = sName, hop, kind, baseClass, filePath = filePathMap.ContainsKey(sName) ? filePathMap[sName] : null, dependsOn, dependedBy, fields, unityCallbacks = callbacks });
        }

        var reachedEdges = codeEdges
            .Where(e => visited.ContainsKey(e.fromObject) && visited.ContainsKey(e.toObject))
            .Select(e => (object)new { from = e.fromObject, to = e.toObject, type = e.fieldType, detail = e.fieldName })
            .ToList();

        var readOrder = TopologicalSort(visited.Keys.ToList(), codeEdges.Where(e => visited.ContainsKey(e.fromObject) && visited.ContainsKey(e.toObject)).ToList(), entryName);

        result.SetValue(new
        {
            success = true,
            entryScript = entryName,
            totalScriptsReached = visited.Count,
            maxHops,
            scripts,
            edges = reachedEdges,
            suggestedReadOrder = readOrder
        });
    }
}
```

## Notes

- BFS expands in both directions: scripts that `scriptName` depends on AND scripts that depend on `scriptName`.
- `suggestedReadOrder` is a topological sort starting from leaf dependencies up to the entry script.
- Use `script_analyze` for a single-class inspection without the full graph.
