# xr_setup_turn_provider

Adds snap or smooth continuous turn locomotion to the XR Origin. Tries the ActionBased variant first, then the generic variant.

**Signature:** `XRSetupTurnProvider(name string = null, instanceId int = 0, path string = null, turnType string = "Snap", turnAmount float = 45, turnSpeed float = 90)`

**Returns:** `{ success, name, instanceId, providerType, turnType, turnAmount, turnSpeed }`

**Notes:**
- `turnType` options: `Snap` (default) or `Continuous`.
- `turnAmount` (degrees per snap step) is only applied when `turnType = "Snap"`.
- `turnSpeed` (degrees per second) is only applied when `turnType = "Continuous"`.
- Comfort default: snap turn with `turnAmount = 45`.

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

                    Undo.RecordObject(go, "Setup Turn Provider");

                    bool isSnap = turnType.Equals("Snap", StringComparison.OrdinalIgnoreCase);
                    Component comp;

                    if (isSnap)
                    {
                        comp = XRReflectionHelper.AddXRComponent(go, "ActionBasedSnapTurnProvider")
                            ?? XRReflectionHelper.AddXRComponent(go, "SnapTurnProvider");
                        if (comp != null)
                            XRReflectionHelper.SetProperty(comp, "turnAmount", turnAmount);
                    }
                    else
                    {
                        comp = XRReflectionHelper.AddXRComponent(go, "ActionBasedContinuousTurnProvider")
                            ?? XRReflectionHelper.AddXRComponent(go, "ContinuousTurnProvider");
                        if (comp != null)
                            XRReflectionHelper.SetProperty(comp, "turnSpeed", turnSpeed);
                    }

                    if (comp == null)
                        { result.SetResult(new { error = $"Failed to add {turnType}TurnProvider. Type not found in current XRI version." }); return; }

                    Undo.RegisterCreatedObjectUndo(comp, "Add Turn Provider");
                    WorkflowManager.SnapshotObject(go);

                    { result.SetResult(new
                    {
                        success = true,
                        name = go.name,
                        instanceId = go.GetInstanceID(),
                        providerType = comp.GetType().Name,
                        turnType,
                        turnAmount = isSnap ? turnAmount : 0f,
                        turnSpeed = isSnap ? 0f : turnSpeed
                    }); return; }
        #endif
    }
}
```
