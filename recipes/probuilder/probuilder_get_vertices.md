# probuilder_get_vertices

Query vertex positions of a ProBuilder mesh. Essential before any vertex edit.

**Signature:** `ProBuilderGetVertices(string name = null, int instanceId = 0, string path = null, string vertexIndexes = null, bool verbose = true)`

**Returns:** `{ success, name, vertexCount, faceCount, vertices: [{ index, x, y, z }] }`

## Notes

- `vertexIndexes`: comma-separated indices to query specific vertices. Omit for all.
- `verbose`: when `true` (default), returns all vertices even on large meshes. When `false` and mesh has more than 100 vertices, returns a summary with bounds instead.
- Read-only — does not modify the mesh.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder.FindOrError(...)`

**Requires:** `com.unity.probuilder` package.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string vertexIndexes = null;
        bool verbose = true;

        var (pbMesh, err) = FindProBuilderMesh(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var positions = pbMesh.positions;
        List<object> vertList;

        if (!string.IsNullOrEmpty(vertexIndexes))
        {
            var indices = ParseIntList(vertexIndexes);
            vertList = new List<object>();
            if (indices != null)
            {
                foreach (var i in indices)
                {
                    if (i >= 0 && i < positions.Count)
                        vertList.Add(new { index = i, x = positions[i].x, y = positions[i].y, z = positions[i].z });
                }
            }
        }
        else if (verbose || positions.Count <= 100)
        {
            vertList = new List<object>();
            for (int i = 0; i < positions.Count; i++)
                vertList.Add(new { index = i, x = positions[i].x, y = positions[i].y, z = positions[i].z });
        }
        else
        {
            var bounds = pbMesh.GetComponent<MeshFilter>()?.sharedMesh?.bounds ?? new Bounds();
            result.SetResult(new
            {
                success = true,
                name = pbMesh.gameObject.name,
                vertexCount = positions.Count,
                bounds = new { min = new { x = bounds.min.x, y = bounds.min.y, z = bounds.min.z }, max = new { x = bounds.max.x, y = bounds.max.y, z = bounds.max.z } },
                note = "Mesh has " + positions.Count + " vertices. Use vertexIndexes to query specific vertices, or verbose=true to get all."
            });
            return;
        }

        result.SetResult(new
        {
            success = true,
            name = pbMesh.gameObject.name,
            vertexCount = positions.Count,
            faceCount = pbMesh.faceCount,
            vertices = vertList
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
