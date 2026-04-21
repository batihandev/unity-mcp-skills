# probuilder_set_vertices

Set absolute positions for specific vertices on a ProBuilder mesh.

**Signature:** `ProBuilderSetVertices(string name = null, int instanceId = 0, string path = null, string vertices = null)`

**Returns:** `{ success, name, instanceId, setVertexCount, totalVertices }`

## Notes

- `vertices`: JSON array of `{ index, x, y, z }` objects (required).
- Out-of-range indices are skipped silently.
- Positions are in local object space.
- Use `probuilder_get_vertices` first to read current positions.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        string vertices = @"[{ ""index"": 0, ""x"": 0, ""y"": 0, ""z"": 0 }]";

        var res = UnitySkillsBridge.Call("probuilder_set_vertices", new { name, vertices });
        result.Log("Set vertices: {0}", res);
    }
}
```
