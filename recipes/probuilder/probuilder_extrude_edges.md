# probuilder_extrude_edges

Extrude open edges outward on a ProBuilder mesh to create walls, rails, or flanges.

**Signature:** `ProBuilderExtrudeEdges(string name = null, int instanceId = 0, string path = null, string edgeIndexes = null, float distance = 0.5f, bool extrudeAsGroup = true, bool enableManifoldExtrude = false)`

**Returns:** `{ success, name, instanceId, extrudedEdgeCount, newEdgeCount, distance, extrudeAsGroup, totalFaces, totalVertices }`

## Notes

- `edgeIndexes` is required; use vertex-index pairs, e.g. `"0-1,2-3"`.
- `distance`: extrude amount in meters (default `0.5`).
- `extrudeAsGroup`: extrude connected edges as a single group (default `true`).
- `enableManifoldExtrude`: allow manifold edge extrusion (default `false`).

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

                    if (string.IsNullOrEmpty(edgeIndexes))
                        { result.SetResult(new { error = "edgeIndexes is required (vertex pairs, e.g. \"0-1,2-3\")" }); return; }

                    var edges = ParseEdgeList(pbMesh, edgeIndexes);
                    if (edges == null || edges.Count == 0)
                        { result.SetResult(new { error = "Invalid edgeIndexes. Use pairs like \"0-1,2-3\" (vertex index pairs)." }); return; }

                    Undo.RecordObject(pbMesh, "Extrude Edges");
                    WorkflowManager.SnapshotObject(pbMesh);

                    var newEdges = pbMesh.Extrude(edges, distance, extrudeAsGroup, enableManifoldExtrude);

                    pbMesh.ToMesh();
                    pbMesh.Refresh();

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        extrudedEdgeCount = edges.Count,
                        newEdgeCount = newEdges?.Length ?? 0,
                        distance,
                        extrudeAsGroup,
                        totalFaces = pbMesh.faceCount,
                        totalVertices = pbMesh.vertexCount
                    }); return; }
        #endif
    }
}
```
