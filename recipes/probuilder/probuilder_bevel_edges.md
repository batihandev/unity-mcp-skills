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
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        #if !PROBUILDER
                    { result.SetResult(NoProBuilder()); return; }
        #else
                    var (pbMesh, err) = FindProBuilderMesh(name, instanceId, path);
                    if (err != null) { result.SetResult(err); return; }

                    if (amount <= 0f || amount > 1f)
                        { result.SetResult(new { error = "amount must be between 0 (exclusive) and 1 (inclusive)" }); return; }

                    IList<Edge> edges;
                    if (string.IsNullOrEmpty(edgeIndexes))
                    {
                        // Bevel all edges
                        var edgeSet = new HashSet<Edge>();
                        foreach (var face in pbMesh.faces)
                            foreach (var edge in face.edges)
                                edgeSet.Add(edge);
                        edges = edgeSet.ToList();
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

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        beveledEdgeCount = edges.Count,
                        newFaceCount = newFaces?.Count ?? 0,
                        amount,
                        totalFaces = pbMesh.faceCount,
                        totalVertices = pbMesh.vertexCount
                    }); return; }
        #endif
    }
}
```
