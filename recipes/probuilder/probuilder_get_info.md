# probuilder_get_info

Get face, vertex, edge, material, and bounds info for a ProBuilder mesh.

**Signature:** `ProBuilderGetInfo(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, name, instanceId, isProBuilder, vertexCount, faceCount, edgeCount, triangleCount, shapeType, position, bounds, materials, submeshFaceCounts }`

## Notes

- Read-only — does not modify the mesh.
- Call this before face or vertex edits to confirm indices and topology.
- `shapeType` is detected via reflection (internal `ProBuilderShape` API).
- `submeshFaceCounts` shows how many faces belong to each submesh/material slot.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";

        var res = UnitySkillsBridge.Call("probuilder_get_info", new { name });
        result.Log("Info: {0}", res);
    }
}
```
