# script_dependency_graph

## Signature

```
ScriptDependencyGraph(
    string scriptName,
    int maxHops = 2,
    bool includeDetails = true)
```

## Return Shape

Returns `success`, `entryScript`, `totalScriptsReached`, `maxHops`, `scripts` array (name, hop, kind, baseClass, filePath, dependsOn, dependedBy, fields, unityCallbacks), `edges` array (from, to, type, detail), `suggestedReadOrder`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`skills_common`](../_shared/skills_common.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class _DepEdge
{
    public string fromObject, toObject, fieldType, fieldName;
    public _DepEdge(string f, string t, string ft, string fn) { fromObject=f; toObject=t; fieldType=ft; fieldName=fn; }
}

internal class CommandScript : IRunCommand
{
    static readonly string[] _UnityCallbacks =
    {
        "Awake","Start","Update","FixedUpdate","LateUpdate","OnEnable","OnDisable","OnDestroy",
        "OnApplicationQuit","OnTriggerEnter","OnTriggerExit","OnTriggerStay",
        "OnCollisionEnter","OnCollisionExit","OnCollisionStay",
        "OnMouseDown","OnMouseUp","OnMouseDrag","OnMouseEnter","OnMouseExit",
        "OnGUI","OnDrawGizmos","OnDrawGizmosSelected","OnValidate","Reset"
    };

    public void Execute(ExecutionResult result)
    {
        string scriptName = "MyScript"; // replace with target class name
        int maxHops = 2;
        bool includeDetails = true;

        if (string.IsNullOrEmpty(scriptName))
        {
            result.SetResult(new { success = false, error = "scriptName is required" });
            return;
        }

        var allTypes = SkillsCommon.GetAllLoadedTypes()
            .Where(t => t.IsClass && IsUserScript(t))
            .ToList();

        var entryType = allTypes.FirstOrDefault(t => t.Name.Equals(scriptName, System.StringComparison.OrdinalIgnoreCase));
        if (entryType == null)
        {
            result.SetResult(new { success = false, error = $"Script '{scriptName}' not found among user scripts" });
            return;
        }

        var entryName = entryType.Name;
        var codeEdges = CollectCodeDependencies(allTypes);

        var outgoing = new Dictionary<string, List<string>>();
        var incoming = new Dictionary<string, List<string>>();
        foreach (var e in codeEdges)
        {
            if (!outgoing.ContainsKey(e.fromObject)) outgoing[e.fromObject] = new List<string>();
            if (!outgoing[e.fromObject].Contains(e.toObject)) outgoing[e.fromObject].Add(e.toObject);
            if (!incoming.ContainsKey(e.toObject)) incoming[e.toObject] = new List<string>();
            if (!incoming[e.toObject].Contains(e.fromObject)) incoming[e.toObject].Add(e.fromObject);
        }

        var visited = new Dictionary<string, int>();
        var bfsQueue = new Queue<string>();
        var bfsHops = new Dictionary<string, int>();
        visited[entryName] = 0; bfsHops[entryName] = 0; bfsQueue.Enqueue(entryName);

        while (bfsQueue.Count > 0)
        {
            var current = bfsQueue.Dequeue();
            var hop = bfsHops[current];
            if (hop >= maxHops) continue;
            if (outgoing.TryGetValue(current, out var outs))
                foreach (var n in outs) if (!visited.ContainsKey(n)) { visited[n] = hop + 1; bfsHops[n] = hop + 1; bfsQueue.Enqueue(n); }
            if (incoming.TryGetValue(current, out var ins))
                foreach (var n in ins) if (!visited.ContainsKey(n)) { visited[n] = hop + 1; bfsHops[n] = hop + 1; bfsQueue.Enqueue(n); }
        }

        var filePathMap = new Dictionary<string, string>();
        foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (ms == null) continue;
            var cls = ms.GetClass();
            if (cls != null && visited.ContainsKey(cls.Name)) filePathMap[cls.Name] = path;
        }

        var typeMap = new Dictionary<string, System.Type>();
        foreach (var t in allTypes)
            if (visited.ContainsKey(t.Name) && !typeMap.ContainsKey(t.Name)) typeMap[t.Name] = t;

        var scripts = new List<object>();
        foreach (var kv in visited.OrderBy(k => k.Value).ThenBy(k => k.Key))
        {
            var sName = kv.Key; var hop = kv.Value;
            var type = typeMap.ContainsKey(sName) ? typeMap[sName] : null;

            var dependsOn = outgoing.ContainsKey(sName) ? outgoing[sName].Where(visited.ContainsKey).OrderBy(n => n).ToList() : new List<string>();
            var dependedBy = incoming.ContainsKey(sName) ? incoming[sName].Where(visited.ContainsKey).OrderBy(n => n).ToList() : new List<string>();

            string kind = null, baseClass = null;
            List<object> fields = null;
            List<string> callbacks = null;

            if (type != null)
            {
                kind = typeof(MonoBehaviour).IsAssignableFrom(type) ? "MonoBehaviour"
                    : typeof(ScriptableObject).IsAssignableFrom(type) ? "ScriptableObject" : "Class";
                baseClass = type.BaseType?.Name;

                if (includeDetails)
                {
                    fields = new List<object>();
                    foreach (var f in type.GetFields())
                    {
                        if (f.DeclaringType != type || f.Name.StartsWith("<")) continue;
                        fields.Add(new { name = f.Name, type = GetFriendlyTypeName(f.FieldType),
                            serializable = f.IsPublic || f.GetCustomAttributes(typeof(SerializeField), false).Length > 0 });
                    }
                    if (typeof(MonoBehaviour).IsAssignableFrom(type))
                    {
                        callbacks = new List<string>();
                        foreach (var m in type.GetMethods())
                            if (m.DeclaringType == type && System.Array.IndexOf(_UnityCallbacks, m.Name) >= 0) callbacks.Add(m.Name);
                    }
                }
            }
            scripts.Add(new { name = sName, hop, kind, baseClass,
                filePath = filePathMap.ContainsKey(sName) ? filePathMap[sName] : null,
                dependsOn, dependedBy, fields, unityCallbacks = callbacks });
        }

        var filteredEdges = codeEdges.Where(e => visited.ContainsKey(e.fromObject) && visited.ContainsKey(e.toObject)).ToList();
        var reachedEdges = filteredEdges.Select(e => (object)new { from = e.fromObject, to = e.toObject, type = e.fieldType, detail = e.fieldName }).ToList();
        var readOrder = TopologicalSort(visited.Keys.ToList(), filteredEdges, entryName);

        result.SetResult(new
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

    bool IsUserScript(System.Type t)
    {
        if (!t.IsClass || t.IsAbstract) return false;
        if (typeof(MonoBehaviour).IsAssignableFrom(t) || typeof(ScriptableObject).IsAssignableFrom(t)) return true;
        if (t.Namespace == null) return false;
        return !t.Namespace.StartsWith("Unity") && !t.Namespace.StartsWith("System") &&
               !t.Namespace.StartsWith("Microsoft") && !t.Namespace.StartsWith("Mono");
    }

    List<_DepEdge> CollectCodeDependencies(List<System.Type> userTypes)
    {
        var userNames = new HashSet<string>();
        foreach (var t in userTypes) userNames.Add(t.Name);

        var edges = new List<_DepEdge>();
        foreach (var t in userTypes)
        {
            foreach (var f in t.GetFields())
            {
                if (!f.IsPublic && f.GetCustomAttributes(typeof(SerializeField), false).Length == 0) continue;
                var ft = f.FieldType;
                if (ft.IsArray) ft = ft.GetElementType();
                if (ft != null && ft.IsGenericType) { var ga = ft.GetGenericArguments(); if (ga.Length > 0) ft = ga[0]; }
                if (ft != null && userNames.Contains(ft.Name) && ft.Name != t.Name)
                    edges.Add(new _DepEdge(t.Name, ft.Name, ft.Name, f.Name));
            }
        }
        return edges;
    }

    List<string> TopologicalSort(List<string> nodes, List<_DepEdge> edges, string entryName)
    {
        var inDeg = new Dictionary<string, int>();
        foreach (var n in nodes) inDeg[n] = 0;
        foreach (var e in edges) if (inDeg.ContainsKey(e.toObject)) inDeg[e.toObject]++;

        var q = new Queue<string>();
        foreach (var n in nodes.OrderBy(x => x)) if (inDeg[n] == 0) q.Enqueue(n);

        var sorted = new List<string>();
        while (q.Count > 0)
        {
            var n = q.Dequeue(); sorted.Add(n);
            foreach (var e in edges)
            {
                if (e.fromObject != n || !inDeg.ContainsKey(e.toObject)) continue;
                if (--inDeg[e.toObject] == 0) q.Enqueue(e.toObject);
            }
        }
        foreach (var n in nodes) if (!sorted.Contains(n)) sorted.Add(n);
        return sorted;
    }

    string GetFriendlyTypeName(System.Type t)
    {
        if (t == null) return "null";
        if (t == typeof(int)) return "int";
        if (t == typeof(float)) return "float";
        if (t == typeof(bool)) return "bool";
        if (t == typeof(string)) return "string";
        if (t == typeof(void)) return "void";
        if (t.IsArray) return GetFriendlyTypeName(t.GetElementType()) + "[]";
        if (t.IsGenericType)
        {
            var args = string.Join(", ", System.Array.ConvertAll(t.GetGenericArguments(), x => GetFriendlyTypeName(x)));
            return t.Name.Split('`')[0] + "<" + args + ">";
        }
        return t.Name;
    }
}
```

## Notes
- BFS expands in both directions: scripts that `scriptName` depends on AND scripts that depend on `scriptName`.
- Use `script_analyze` for a single-class inspection without the full graph.

