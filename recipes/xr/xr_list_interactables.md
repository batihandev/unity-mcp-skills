# xr_list_interactables

Lists all XR interactables in the scene (XRGrabInteractable, XRSimpleInteractable, TeleportationArea, TeleportationAnchor) with type, path, and selection/hover state.

**Signature:** `XRListInteractables(verbose bool = false)`

**Returns:** `{ success, count, interactables, xriVersion }`

**Notes:**
- TeleportationArea and TeleportationAnchor are included because they implement XRBaseInteractable.
- Pass `verbose=true` to include a `properties` map for grab and simple interactable entries.
- Read-only; does not modify the scene.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool verbose = false;

        var res = UnitySkillsBridge.Call("xr_list_interactables", new { verbose });
        result.SetResult(res);
    }
}
```
