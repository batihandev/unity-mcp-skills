# probuilder_center_pivot

Center the pivot to mesh bounds, or move it to a specific world position.

**Signature:** `ProBuilderCenterPivot(string name = null, int instanceId = 0, string path = null, float? worldX = null, float? worldY = null, float? worldZ = null)`

**Returns:** `{ success, name, instanceId, pivot: { x, y, z } }`

## Notes

- Omit `worldX/Y/Z` to auto-center the pivot at the mesh bounds center.
- Provide any combination of `worldX`, `worldY`, `worldZ` to pin individual axes while auto-centering the rest.
- Adjusting the pivot shifts the transform position without moving the visual mesh in the scene.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        float? worldX = null, worldY = null, worldZ = null; // null = auto-center

        var res = UnitySkillsBridge.Call("probuilder_center_pivot", new { name, worldX, worldY, worldZ });
        result.Log("Pivot: {0}", res);
    }
}
```
