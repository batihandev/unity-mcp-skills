# probuilder_bridge_edges

Connect two edges with a new face on a ProBuilder mesh (create doorways, windows, or connections).

**Signature:** `ProBuilderBridgeEdges(string name = null, int instanceId = 0, string path = null, string edgeA = null, string edgeB = null, bool allowNonManifold = false)`

**Returns:** `{ success, name, instanceId, bridgedEdge: { a, b }, totalFaces, totalVertices }`

## Notes

- `edgeA` and `edgeB` are both required; use vertex-index pairs, e.g. `"0-1"` and `"4-5"`.
- `allowNonManifold`: allow bridging in cases that produce non-manifold geometry (default `false`).
- Returns an error if either edge is not found or the bridge fails.

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

                    if (string.IsNullOrEmpty(edgeA) || string.IsNullOrEmpty(edgeB))
                        { result.SetResult(new { error = "Both edgeA and edgeB are required (e.g. edgeA=\"0-1\", edgeB=\"4-5\")" }); return; }

                    var edgesA = ParseEdgeList(pbMesh, edgeA);
                    var edgesB = ParseEdgeList(pbMesh, edgeB);
                    if (edgesA == null || edgesA.Count == 0 || edgesB == null || edgesB.Count == 0)
                        { result.SetResult(new { error = "Invalid edge format. Use \"vertexA-vertexB\" (e.g. \"0-1\")." }); return; }

                    Undo.RecordObject(pbMesh, "Bridge Edges");
                    WorkflowManager.SnapshotObject(pbMesh);

                    var newFace = pbMesh.Bridge(edgesA[0], edgesB[0], allowNonManifold);
                    if (newFace == null)
                        { result.SetResult(new { error = "Failed to bridge edges. Ensure both edges exist and can be connected." }); return; }

                    pbMesh.ToMesh();
                    pbMesh.Refresh();

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        bridgedEdge = new { a = edgeA, b = edgeB },
                        totalFaces = pbMesh.faceCount,
                        totalVertices = pbMesh.vertexCount
                    }); return; }
        #endif
    }
}
```
