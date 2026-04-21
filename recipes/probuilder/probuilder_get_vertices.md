# probuilder_get_vertices

Query vertex positions of a ProBuilder mesh. Essential before any vertex edit.

**Signature:** `ProBuilderGetVertices(string name = null, int instanceId = 0, string path = null, string vertexIndexes = null, bool verbose = true)`

**Returns:** `{ success, name, vertexCount, faceCount, vertices: [{ index, x, y, z }] }`

## Notes

- `vertexIndexes`: comma-separated indices to query specific vertices. Omit for all.
- `verbose`: when `true` (default), returns all vertices even on large meshes. When `false` and mesh has more than 100 vertices, returns a summary with bounds instead.
- Read-only — does not modify the mesh.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        string vertexIndexes = null; // null = all
        bool verbose = true;

        var res = UnitySkillsBridge.Call("probuilder_get_vertices", new { name, vertexIndexes, verbose });
        result.Log("Vertices: {0}", res);
    }
}
```
