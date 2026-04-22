# probuilder_bevel_edges

Chamfer (bevel) edges on a ProBuilder mesh.

**Signature:** `ProBuilderBevelEdges(string name = null, int instanceId = 0, string path = null, string edgeIndexes = null, float amount = 0.2f)`

**Returns:** `{ success, name, instanceId, beveledEdgeCount, newFaceCount, amount, totalFaces, totalVertices }`

## Notes

- `edgeIndexes`: vertex-index pairs, e.g. `"0-1,2-3"`. Omit to bevel all edges.
- `amount`: bevel width factor in range `(0, 1]` (default `0.2`).
- Returns an error if `amount <= 0` or `amount > 1`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

**Requires:** `com.unity.probuilder` package.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string edgeIndexes = null;
        float amount = 0.2f;

        var (pbMesh, err) = FindProBuilderMesh(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        if (amount <= 0f || amount > 1f)
        { result.SetResult(new { error = "amount must be between 0 (exclusive) and 1 (inclusive)" }); return; }

        IList<Edge> edges;
        if (string.IsNullOrEmpty(edgeIndexes))
        {
            // HashSet<Edge> triggers CS0012 (ISet<> needs System.dll). Dedupe via Distinct on a flat list.
            var all = new List<Edge>();
            foreach (var face in pbMesh.faces)
                foreach (var edge in face.edges)
                    all.Add(edge);
            edges = all.Distinct().ToList();
        }
        else
        {
            edges = ParseEdgeList(pbMesh, edgeIndexes);
            if (edges == null || edges.Count == 0)
            { result.SetResult(new { error = "Invalid edgeIndexes. Use pairs like \"0-1,2-3\" (vertex index pairs)." }); return; }
        }

        Undo.RecordObject(pbMesh, "Bevel Edges");
        WorkflowManager.SnapshotObject(pbMesh);

        var newFaces = Bevel.BevelEdges(pbMesh, edges, amount);

        pbMesh.ToMesh();
        pbMesh.Refresh();

        result.SetResult(new
        {
            success = true,
            name = pbMesh.gameObject.name,
            instanceId = pbMesh.gameObject.GetInstanceID(),
            beveledEdgeCount = edges.Count,
            newFaceCount = newFaces?.Count ?? 0,
            amount,
            totalFaces = pbMesh.faceCount,
            totalVertices = pbMesh.vertexCount
        });
    }

    private static (ProBuilderMesh mesh, object error) FindProBuilderMesh(string name, int instanceId, string path)
    {
        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) return (null, findErr);

        var pbMesh = go.GetComponent<ProBuilderMesh>();
        if (pbMesh == null)
            return (null, new { error = "GameObject '" + go.name + "' does not have a ProBuilderMesh component" });

        return (pbMesh, null);
    }

    private static IList<Edge> ParseEdgeList(ProBuilderMesh mesh, string edgeIndexes)
    {
        if (string.IsNullOrEmpty(edgeIndexes)) return null;
        var edges = new List<Edge>();
        foreach (var pair in edgeIndexes.Split(','))
        {
            var parts = pair.Trim().Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out var a) && int.TryParse(parts[1].Trim(), out var b))
                edges.Add(new Edge(a, b));
        }
        return edges.Count > 0 ? edges : null;
    }
}
```
