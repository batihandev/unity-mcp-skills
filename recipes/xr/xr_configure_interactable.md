# xr_configure_interactable

Fine-tunes properties on an existing XR interactable (XRGrabInteractable or XRSimpleInteractable). Pass only the fields you want to change.

**Signature:** `XRConfigureInteractable(name string = null, instanceId int = 0, path string = null, selectMode string = null, movementType string = null, throwOnDetach bool? = null, smoothPosition bool? = null, smoothRotation bool? = null, smoothPositionAmount float? = null, smoothRotationAmount float? = null, trackPosition bool? = null, trackRotation bool? = null)`

**Returns:** `{ success, name, instanceId, interactableType, changedProperties, selectModeOptions, movementTypeOptions }`

**Notes:**
- All parameters except the target selector are optional — omit unchanged fields.
- `changedProperties` lists only the properties that were actually updated via reflection.

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
                    var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
                    if (findErr != null) { result.SetResult(findErr); return; }

                    // Find any interactable component
                    var comp = XRReflectionHelper.GetXRComponent(go, "XRGrabInteractable")
                            ?? XRReflectionHelper.GetXRComponent(go, "XRSimpleInteractable")
                            ?? XRReflectionHelper.GetXRComponent(go, "XRBaseInteractable");

                    if (comp == null)
                        { result.SetResult(new { error = $"No XR interactable found on '{go.name}'. Add one via xr_add_grab_interactable or xr_add_simple_interactable." }); return; }

                    Undo.RecordObject(comp, "Configure XR Interactable");
                    WorkflowManager.SnapshotObject(comp);

                    var changed = new List<string>();

                    if (!string.IsNullOrEmpty(selectMode) && XRReflectionHelper.SetEnumProperty(comp, "selectMode", selectMode))
                        changed.Add("selectMode");
                    if (!string.IsNullOrEmpty(movementType) && XRReflectionHelper.SetEnumProperty(comp, "movementType", movementType))
                        changed.Add("movementType");
                    if (throwOnDetach.HasValue && XRReflectionHelper.SetProperty(comp, "throwOnDetach", throwOnDetach.Value))
                        changed.Add("throwOnDetach");
                    if (smoothPosition.HasValue && XRReflectionHelper.SetProperty(comp, "smoothPosition", smoothPosition.Value))
                        changed.Add("smoothPosition");
                    if (smoothRotation.HasValue && XRReflectionHelper.SetProperty(comp, "smoothRotation", smoothRotation.Value))
                        changed.Add("smoothRotation");
                    if (smoothPositionAmount.HasValue && XRReflectionHelper.SetProperty(comp, "smoothPositionAmount", smoothPositionAmount.Value))
                        changed.Add("smoothPositionAmount");
                    if (smoothRotationAmount.HasValue && XRReflectionHelper.SetProperty(comp, "smoothRotationAmount", smoothRotationAmount.Value))
                        changed.Add("smoothRotationAmount");
                    if (trackPosition.HasValue && XRReflectionHelper.SetProperty(comp, "trackPosition", trackPosition.Value))
                        changed.Add("trackPosition");
                    if (trackRotation.HasValue && XRReflectionHelper.SetProperty(comp, "trackRotation", trackRotation.Value))
                        changed.Add("trackRotation");

                    { result.SetResult(new
                    {
                        success = true,
                        name = go.name,
                        instanceId = go.GetInstanceID(),
                        interactableType = comp.GetType().Name,
                        changedProperties = changed,
                        selectModeOptions = XRReflectionHelper.GetEnumValues(comp, "selectMode"),
                        movementTypeOptions = XRReflectionHelper.GetEnumValues(comp, "movementType")
                    }); return; }
        #endif
    }
}
```
