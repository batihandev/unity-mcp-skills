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
                        { result.SetResult(new { error = $"No valid vertex indices. Mesh has {positions.Count} vertices (0-{positions.Count - 1})." }); return; }

                    Undo.RecordObject(pbMesh, "Weld Vertices");
                    WorkflowManager.SnapshotObject(pbMesh);

                    var weldedIndices = pbMesh.WeldVertices(validIndices, radius);

                    pbMesh.ToMesh();
                    pbMesh.Refresh();

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        inputVertexCount = validIndices.Count,
                        weldedVertexCount = weldedIndices?.Length ?? 0,
                        radius,
                        totalVertices = pbMesh.vertexCount
                    }); return; }
        #endif
    }
}
```
