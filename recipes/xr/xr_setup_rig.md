# xr_setup_rig

Creates a complete XR Origin rig with Camera Offset, Main Camera, Left Controller, and Right Controller, and adds TrackedPoseDriver to each. Also adds XRInteractionManager if none exists.

**Signature:** `XRSetupRig(name string = "XR Origin", x float = 0, y float = 0, z float = 0, cameraYOffset float = 1.36144)`

**Returns:** `{ success, name, instanceId, xriVersion, hierarchy, position, cameraYOffset, note }`

**Notes:**
- Requires `com.unity.xr.core-utils` in addition to XRI.
- `cameraYOffset` sets standing eye height on the Camera Offset child object.
- After this call, add interactors via `xr_add_ray_interactor` or `xr_add_direct_interactor`.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "XR Origin";
        float x = 0f, y = 0f, z = 0f;
        float cameraYOffset = 1.36144f;

        var res = UnitySkillsBridge.Call("xr_setup_rig", new { name, x, y, z, cameraYOffset });
        result.SetResult(res);
    }
}
```
