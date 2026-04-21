# probuilder_move_vertices

Offset vertices by a delta vector on a ProBuilder mesh.

**Signature:** `ProBuilderMoveVertices(string name = null, int instanceId = 0, string path = null, string vertexIndexes = null, float deltaX = 0, float deltaY = 0, float deltaZ = 0)`

**Returns:** `{ success, name, instanceId, movedVertexCount, delta: { x, y, z }, totalVertices }`

## Notes

- `vertexIndexes` is required; comma-separated vertex indices, e.g. `"4,5,6,7"` for the top vertices of a Cube.
- Use `probuilder_get_vertices` first to identify correct indices.
- Delta is applied in local object space.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        string vertexIndexes = "4,5,6,7"; // e.g. top vertices of a Cube
        float deltaX = 0f, deltaY = 0.5f, deltaZ = 0f;

        var res = UnitySkillsBridge.Call("probuilder_move_vertices", new {
            name, vertexIndexes, deltaX, deltaY, deltaZ
        });
        result.Log("Moved vertices: {0}", res);
    }
}
```
