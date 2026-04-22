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
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder.FindOrError(...)`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

**Requires:** `com.unity.probuilder` package.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string edgeA = null;
        string edgeB = null;
        bool allowNonManifold = false;

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

        result.SetResult(new
        {
            success = true,
            name = pbMesh.gameObject.name,
            instanceId = pbMesh.gameObject.GetInstanceID(),
            bridgedEdge = new { a = edgeA, b = edgeB },
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
