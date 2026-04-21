# xr_setup_teleportation

Adds TeleportationProvider to the XR Origin. Auto-finds XR Origin if no target is specified.

**Signature:** `XRSetupTeleportation(name string = null, instanceId int = 0, path string = null)`

**Returns:** `{ success, name, instanceId, providerType, note }`

**Notes:**
- Omit all selector parameters to auto-target the XR Origin in the scene.
- After this call, create teleport destinations with `xr_add_teleport_area` or `xr_add_teleport_anchor`.
- Requires an XR Origin to already exist; create one with `xr_setup_rig` first.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Omit name/instanceId/path to target XR Origin automatically
        var res = UnitySkillsBridge.Call("xr_setup_teleportation", new { });
        result.SetResult(res);
    }
}
```
