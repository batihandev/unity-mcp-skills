# xr_add_teleport_area

Marks a surface as a teleport destination by adding TeleportationArea. Auto-adds a Collider if none exists.

**Signature:** `XRAddTeleportArea(name string = null, instanceId int = 0, path string = null, matchOrientation string = "WorldSpaceUp")`

**Returns:** `{ success, name, instanceId, teleportType, matchOrientation, matchOrientationOptions }`

**Notes:**
- `matchOrientation` options: `WorldSpaceUp` (default), `TargetUp`, `TargetUpAndForward`, `None`.
- If a MeshFilter with a mesh is present, a MeshCollider is added; otherwise a BoxCollider.
- The collider must NOT be a trigger — the raycast hits it as a surface.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Floor";
        string matchOrientation = "WorldSpaceUp";

        var res = UnitySkillsBridge.Call("xr_add_teleport_area", new { name, matchOrientation });
        result.SetResult(res);
    }
}
```
