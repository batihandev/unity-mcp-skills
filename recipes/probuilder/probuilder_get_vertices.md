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

                    var positions = pbMesh.positions;
                    List<object> result;

                    if (!string.IsNullOrEmpty(vertexIndexes))
                    {
                        var indices = ParseIntList(vertexIndexes);
                        result = (indices ?? new List<int>())
                            .Where(i => i >= 0 && i < positions.Count)
                            .Select(i => (object)new { index = i, x = positions[i].x, y = positions[i].y, z = positions[i].z })
                            .ToList();
                    }
                    else if (verbose || positions.Count <= 100)
                    {
                        result = new List<object>();
                        for (int i = 0; i < positions.Count; i++)
                            result.Add(new { index = i, x = positions[i].x, y = positions[i].y, z = positions[i].z });
                    }
                    else
                    {
                        // Summary mode for large meshes
                        var bounds = pbMesh.GetComponent<MeshFilter>()?.sharedMesh?.bounds ?? new Bounds();
                        { result.SetResult(new
                        {
                            success = true,
                            name = pbMesh.gameObject.name,
                            vertexCount = positions.Count,
                            bounds = new { min = new { x = bounds.min.x, y = bounds.min.y, z = bounds.min.z }, max = new { x = bounds.max.x, y = bounds.max.y, z = bounds.max.z } },
                            note = $"Mesh has {positions.Count} vertices. Use vertexIndexes to query specific vertices, or verbose=true to get all."
                        }); return; }
                    }

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        vertexCount = positions.Count,
                        faceCount = pbMesh.faceCount,
                        vertices = result
                    }); return; }
        #endif
    }
}
```
