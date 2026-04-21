# xr_add_teleport_anchor

Creates a stationary teleport destination at a specific position and rotation. Adds TeleportationAnchor, a thin BoxCollider for raycast detection, and a flat cylinder visual indicator.

**Signature:** `XRAddTeleportAnchor(name string = "Teleport Anchor", x float = 0, y float = 0, z float = 0, rotY float = 0, matchOrientation string = "TargetUpAndForward", parent string = null)`

**Returns:** `{ success, name, instanceId, teleportType, position, rotationY, matchOrientation }`

**Notes:**
- `matchOrientation` defaults to `TargetUpAndForward` to face the player toward the anchor's forward direction.
- The BoxCollider has size `(1, 0.01, 1)` — a flat detection surface at floor level.
- The cylinder visual child ("Anchor Visual") has its auto-generated collider removed and is scaled `(1, 0.02, 1)`.
- `parent`: name of a GameObject to parent the anchor under (optional).

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Teleport Anchor";
        float x = 0f, y = 0f, z = 5f;
        float rotY = 0f;
        string matchOrientation = "TargetUpAndForward";
        string parent = null;

        var res = UnitySkillsBridge.Call("xr_add_teleport_anchor", new {
            name, x, y, z, rotY, matchOrientation, parent
        });
        result.SetResult(res);
    }
}
```
