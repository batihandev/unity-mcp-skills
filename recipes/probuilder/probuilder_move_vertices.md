# probuilder_move_vertices

Offset vertices by a delta vector on a ProBuilder mesh.

**Signature:** `ProBuilderMoveVertices(string name = null, int instanceId = 0, string path = null, string vertexIndexes = null, float deltaX = 0, float deltaY = 0, float deltaZ = 0)`

**Returns:** `{ success, name, instanceId, movedVertexCount, delta: { x, y, z }, totalVertices }`

## Notes

- `vertexIndexes` is required; comma-separated vertex indices, e.g. `"4,5,6,7"` for the top vertices of a Cube.
- Use `probuilder_get_vertices` first to identify correct indices.
- Delta is applied in local object space.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.probuilder` package.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string vertexIndexes = null;
        float deltaX = 0, deltaY = 0, deltaZ = 0;

        var (pbMesh, err) = FindProBuilderMesh(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        if (string.IsNullOrEmpty(vertexIndexes))
        { result.SetResult(new { error = "vertexIndexes is required (comma-separated, e.g. \"4,5,6,7\" for top vertices of a Cube)" }); return; }

        var indices = ParseIntList(vertexIndexes);
        if (indices == null || indices.Count == 0)
        { result.SetResult(new { error = "Invalid vertexIndexes format" }); return; }

        var positions = pbMesh.positions;
        var validIndices = indices.Where(i => i >= 0 && i < positions.Count).ToList();
        if (validIndices.Count == 0)
        { result.SetResult(new { error = "No valid vertex indices. Mesh has " + positions.Count + " vertices (0-" + (positions.Count - 1) + ")." }); return; }

        Undo.RecordObject(pbMesh, "Move Vertices");
        WorkflowManager.SnapshotObject(pbMesh);

        var delta = new Vector3(deltaX, deltaY, deltaZ);
        var newPositions = positions.ToArray();
        foreach (var idx in validIndices)
            newPositions[idx] += delta;
        pbMesh.positions = newPositions;

        pbMesh.ToMesh();
        pbMesh.Refresh();

        result.SetResult(new
        {
            success = true,
            name = pbMesh.gameObject.name,
            instanceId = pbMesh.gameObject.GetInstanceID(),
            movedVertexCount = validIndices.Count,
            delta = new { x = deltaX, y = deltaY, z = deltaZ },
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

    private static List<int> ParseIntList(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return null;
        var list = new List<int>();
        foreach (var part in csv.Split(','))
        {
            if (int.TryParse(part.Trim(), out var val))
                list.Add(val);
        }
        return list.Count > 0 ? list : null;
    }
}
```
