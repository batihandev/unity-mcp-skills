# probuilder_weld_vertices

Merge vertices within a given radius on a ProBuilder mesh.

**Signature:** `ProBuilderWeldVertices(string name = null, int instanceId = 0, string path = null, string vertexIndexes = null, float radius = 0.01f)`

**Returns:** `{ success, name, instanceId, inputVertexCount, weldedVertexCount, radius, totalVertices }`

## Notes

- `vertexIndexes` is required; comma-separated vertex indices, e.g. `"0,1,2,3"`.
- `radius`: weld threshold distance (default `0.01`). Must be `> 0`.
- Use `probuilder_get_vertices` first to identify overlapping vertices before welding.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        string vertexIndexes = "0,1,2,3";
        float radius = 0.01f;

        var res = UnitySkillsBridge.Call("probuilder_weld_vertices", new { name, vertexIndexes, radius });
        result.Log("Welded: {0}", res);
    }
}
```
