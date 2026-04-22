# probuilder_weld_vertices

Merge vertices within a given radius on a ProBuilder mesh.

**Signature:** `ProBuilderWeldVertices(string name = null, int instanceId = 0, string path = null, string vertexIndexes = null, float radius = 0.01f)`

**Returns:** `{ success, name, instanceId, inputVertexCount, weldedVertexCount, radius, totalVertices }`

## Notes

- `vertexIndexes` is required; comma-separated vertex indices, e.g. `"0,1,2,3"`.
- `radius`: weld threshold distance (default `0.01`). Must be `> 0`.
- Use `probuilder_get_vertices` first to identify overlapping vertices before welding.

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
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string vertexIndexes = null;
        float radius = 0.01f;

        var (pbMesh, err) = FindProBuilderMesh(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        if (string.IsNullOrEmpty(vertexIndexes))
        { result.SetResult(new { error = "vertexIndexes is required (comma-separated, e.g. \"0,1,2,3\")" }); return; }

        var indices = ParseIntList(vertexIndexes);
        if (indices == null || indices.Count == 0)
        { result.SetResult(new { error = "Invalid vertexIndexes format" }); return; }

        if (radius <= 0f)
        { result.SetResult(new { error = "radius must be greater than 0" }); return; }

        var positions = pbMesh.positions;
        var validIndices = indices.Where(i => i >= 0 && i < positions.Count).ToList();
        if (validIndices.Count == 0)
        { result.SetResult(new { error = "No valid vertex indices. Mesh has " + positions.Count + " vertices (0-" + (positions.Count - 1) + ")." }); return; }

        Undo.RecordObject(pbMesh, "Weld Vertices");
        WorkflowManager.SnapshotObject(pbMesh);

        var weldedIndices = pbMesh.WeldVertices(validIndices, radius);

        pbMesh.ToMesh();
        pbMesh.Refresh();

        result.SetResult(new
        {
            success = true,
            name = pbMesh.gameObject.name,
            instanceId = pbMesh.gameObject.GetInstanceID(),
            inputVertexCount = validIndices.Count,
            weldedVertexCount = weldedIndices?.Length ?? 0,
            radius,
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
