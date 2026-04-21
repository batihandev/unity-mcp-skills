# xr_setup_ui_canvas

Converts an existing Canvas to WorldSpace rendering and adds TrackedDeviceGraphicRaycaster. Removes the standard GraphicRaycaster if present. Sets canvas size to 400x300 at scale 0.001.

**Signature:** `XRSetupUICanvas(name string = null, instanceId int = 0, path string = null)`

**Returns:** `{ success, name, instanceId, removedStandardRaycaster, addedTrackedDeviceRaycaster, renderMode, note }`

**Notes:**
- The target GameObject must already have a Canvas component.
- After conversion, ensure `xr_setup_event_system` has been called and a ray interactor is on the controller.
- `note` is set if TrackedDeviceGraphicRaycaster could not be found (XRI UI module may be missing).

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyCanvas";

        var res = UnitySkillsBridge.Call("xr_setup_ui_canvas", new { name });
        result.SetResult(res);
    }
}
```
