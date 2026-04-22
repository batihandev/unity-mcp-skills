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
        string edgeIndexes = null;
        float distance = 0.5f;
        bool extrudeAsGroup = true;
        bool enableManifoldExtrude = false;

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

        result.SetResult(new
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
        });
    }

    private static (ProBuilderMesh mesh, object error) FindProBuilderMesh(string fname, int fid, string fpath)
    {
        var (go, findErr) = GameObjectFinder.FindOrError(fname, fid, fpath);
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
