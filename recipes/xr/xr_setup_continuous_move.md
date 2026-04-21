# xr_setup_continuous_move

Adds continuous stick locomotion to the XR Origin. Tries ActionBasedContinuousMoveProvider first, then falls back to ContinuousMoveProvider.

**Signature:** `XRSetupContinuousMove(name string = null, instanceId int = 0, path string = null, moveSpeed float = 2.0, enableStrafe bool = true, enableFly bool = false)`

**Returns:** `{ success, name, instanceId, providerType, moveSpeed, enableStrafe, enableFly }`

**Notes:**
- Omit all selector parameters to auto-target the XR Origin in the scene.
- `moveSpeed` is in meters per second; comfort default is `2.0`.
- `enableFly`: when true, disables gravity-locked movement.

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

                    Undo.RecordObject(go, "Setup Continuous Move");

                    // Try ActionBased first, then generic
                    var comp = XRReflectionHelper.AddXRComponent(go, "ActionBasedContinuousMoveProvider")
                            ?? XRReflectionHelper.AddXRComponent(go, "ContinuousMoveProvider");

                    if (comp == null)
                        { result.SetResult(new { error = "Failed to add ContinuousMoveProvider. Type not found in current XRI version." }); return; }

                    Undo.RegisterCreatedObjectUndo(comp, "Add ContinuousMoveProvider");

                    XRReflectionHelper.SetProperty(comp, "moveSpeed", moveSpeed);
                    XRReflectionHelper.SetProperty(comp, "enableStrafe", enableStrafe);
                    XRReflectionHelper.SetProperty(comp, "enableFly", enableFly);

                    WorkflowManager.SnapshotObject(go);

                    { result.SetResult(new
                    {
                        success = true,
                        name = go.name,
                        instanceId = go.GetInstanceID(),
                        providerType = comp.GetType().Name,
                        moveSpeed,
                        enableStrafe,
                        enableFly
                    }); return; }
        #endif
    }
}
```
