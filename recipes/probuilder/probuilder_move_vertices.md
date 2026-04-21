# probuilder_move_vertices

Offset vertices by a delta vector on a ProBuilder mesh.

**Signature:** `ProBuilderMoveVertices(string name = null, int instanceId = 0, string path = null, string vertexIndexes = null, float deltaX = 0, float deltaY = 0, float deltaZ = 0)`

**Returns:** `{ success, name, instanceId, movedVertexCount, delta: { x, y, z }, totalVertices }`

## Notes

- `vertexIndexes` is required; comma-separated vertex indices, e.g. `"4,5,6,7"` for the top vertices of a Cube.
- Use `probuilder_get_vertices` first to identify correct indices.
- Delta is applied in local object space.

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
                        { result.SetResult(new { error = "vertexIndexes is required (comma-separated, e.g. \"4,5,6,7\" for top vertices of a Cube)" }); return; }

                    var indices = ParseIntList(vertexIndexes);
                    if (indices == null || indices.Count == 0)
                        { result.SetResult(new { error = "Invalid vertexIndexes format" }); return; }

                    var positions = pbMesh.positions;
                    var validIndices = indices.Where(i => i >= 0 && i < positions.Count).ToList();
                    if (validIndices.Count == 0)
                        { result.SetResult(new { error = $"No valid vertex indices. Mesh has {positions.Count} vertices (0-{positions.Count - 1})." }); return; }

                    Undo.RecordObject(pbMesh, "Move Vertices");
                    WorkflowManager.SnapshotObject(pbMesh);

                    var delta = new Vector3(deltaX, deltaY, deltaZ);
                    var newPositions = positions.ToArray();
                    foreach (var idx in validIndices)
                        newPositions[idx] += delta;
                    pbMesh.positions = newPositions;

                    pbMesh.ToMesh();
                    pbMesh.Refresh();

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        movedVertexCount = validIndices.Count,
                        delta = new { x = deltaX, y = deltaY, z = deltaZ },
                        totalVertices = pbMesh.vertexCount
                    }); return; }
        #endif
    }
}
```
