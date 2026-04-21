# xr_add_grab_interactable

Makes a GameObject grabbable by adding XRGrabInteractable. Auto-adds Rigidbody and Collider if missing. The collider must not be a trigger.

**Signature:** `XRAddGrabInteractable(name string = null, instanceId int = 0, path string = null, movementType string = "VelocityTracking", throwOnDetach bool = true, smoothPosition bool = true, smoothRotation bool = true, smoothPositionAmount float = 5, smoothRotationAmount float = 5, useGravity bool = true, isKinematic bool = false, attachTransformOffset string = null)`

**Returns:** `{ success, name, instanceId, movementType, throwOnDetach, smoothPosition, smoothRotation, movementTypeOptions }`

**Notes:**
- `movementType` options: `VelocityTracking` (default, best for physics), `Kinematic` (handles/tools), `Instantaneous` (precise remote grab).
- `attachTransformOffset`: comma-separated `"x,y,z"` string that creates a child Attach Point transform.
- If the GameObject has a MeshFilter with a mesh, a convex MeshCollider is added; otherwise a BoxCollider.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyCube";
        string movementType = "VelocityTracking";
        bool throwOnDetach = true;
        bool smoothPosition = true;
        bool smoothRotation = true;
        float smoothPositionAmount = 5f;
        float smoothRotationAmount = 5f;
        bool useGravity = true;
        bool isKinematic = false;
        string attachTransformOffset = null;

        var res = UnitySkillsBridge.Call("xr_add_grab_interactable", new {
            name, movementType, throwOnDetach, smoothPosition, smoothRotation,
            smoothPositionAmount, smoothRotationAmount, useGravity, isKinematic, attachTransformOffset
        });
        result.SetResult(res);
    }
}
```
