# xr_setup_teleportation

Adds TeleportationProvider to the XR Origin. Auto-finds XR Origin if no target is specified.

**Signature:** `XRSetupTeleportation(name string = null, instanceId int = 0, path string = null)`

**Returns:** `{ success, name, instanceId, providerType, note }`

**Notes:**
- Omit all selector parameters to auto-target the XR Origin in the scene.
- After this call, create teleport destinations with `xr_add_teleport_area` or `xr_add_teleport_anchor`.
- Requires an XR Origin to already exist; create one with `xr_setup_rig` first.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        #if !XRI
                    { result.SetResult(NoXRI()); return; }
        #else
                    // Find XR Origin
                    GameObject go;
                    if (string.IsNullOrEmpty(name) && instanceId == 0 && string.IsNullOrEmpty(path))
                    {
                        var origin = XRReflectionHelper.FindFirstOfXRType("XROrigin");
                        if (origin == null)
                            { result.SetResult(new { error = "No XR Origin found in scene. Create one via xr_setup_rig, or specify the target object." }); return; }
                        go = origin.gameObject;
                    }
                    else
                    {
                        var (found, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
                        if (findErr != null) { result.SetResult(findErr); return; }
                        go = found;
                    }

                    Undo.RecordObject(go, "Setup Teleportation");

                    var comp = XRReflectionHelper.AddXRComponent(go, "TeleportationProvider");
                    if (comp == null)
                        { result.SetResult(new { error = "Failed to add TeleportationProvider. Type not found in current XRI version." }); return; }

                    Undo.RegisterCreatedObjectUndo(comp, "Add TeleportationProvider");
                    WorkflowManager.SnapshotObject(go);

                    { result.SetResult(new
                    {
                        success = true,
                        name = go.name,
                        instanceId = go.GetInstanceID(),
                        providerType = comp.GetType().Name,
                        note = "Now create teleport targets via xr_add_teleport_area or xr_add_teleport_anchor."
                    }); return; }
        #endif
    }
}
```
