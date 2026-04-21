# xr_configure_haptics

Sets haptic feedback intensity and duration for select and hover events on an XR interactor.

**Signature:** `XRConfigureHaptics(name string = null, instanceId int = 0, path string = null, selectIntensity float = 0.5, selectDuration float = 0.1, hoverIntensity float = 0.1, hoverDuration float = 0.05)`

**Returns:** `{ success, name, instanceId, interactorType, changedProperties, selectIntensity, selectDuration, hoverIntensity, hoverDuration, note }`

**Notes:**
- Properties are set via reflection; `changedProperties` lists only those successfully applied.
- If `changedProperties` is empty, the haptic API may differ in the installed XRI version.
- Works on XRRayInteractor, XRDirectInteractor, XRSocketInteractor, or XRBaseInteractor.

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

                    // Find any interactor component
                    var comp = XRReflectionHelper.GetXRComponent(go, "XRRayInteractor")
                            ?? XRReflectionHelper.GetXRComponent(go, "XRDirectInteractor")
                            ?? XRReflectionHelper.GetXRComponent(go, "XRSocketInteractor")
                            ?? XRReflectionHelper.GetXRComponent(go, "XRBaseInteractor");

                    if (comp == null)
                        { result.SetResult(new { error = $"No XR interactor found on '{go.name}'." }); return; }

                    Undo.RecordObject(comp, "Configure Haptics");
                    WorkflowManager.SnapshotObject(comp);

                    var changed = new List<string>();

                    // Haptic properties vary by version but try common names
                    if (XRReflectionHelper.SetProperty(comp, "playHapticsOnSelectEntered", true))
                        changed.Add("playHapticsOnSelectEntered");
                    if (XRReflectionHelper.SetProperty(comp, "hapticSelectEnterIntensity", selectIntensity))
                        changed.Add("hapticSelectEnterIntensity");
                    if (XRReflectionHelper.SetProperty(comp, "hapticSelectEnterDuration", selectDuration))
                        changed.Add("hapticSelectEnterDuration");
                    if (XRReflectionHelper.SetProperty(comp, "playHapticsOnHoverEntered", hoverIntensity > 0))
                        changed.Add("playHapticsOnHoverEntered");
                    if (XRReflectionHelper.SetProperty(comp, "hapticHoverEnterIntensity", hoverIntensity))
                        changed.Add("hapticHoverEnterIntensity");
                    if (XRReflectionHelper.SetProperty(comp, "hapticHoverEnterDuration", hoverDuration))
                        changed.Add("hapticHoverEnterDuration");

                    { result.SetResult(new
                    {
                        success = true,
                        name = go.name,
                        instanceId = go.GetInstanceID(),
                        interactorType = comp.GetType().Name,
                        changedProperties = changed,
                        selectIntensity,
                        selectDuration,
                        hoverIntensity,
                        hoverDuration,
                        note = changed.Count == 0 ? "Haptic properties not found on this interactor type. Haptics API may differ in your XRI version." : null
                    }); return; }
        #endif
    }
}
```
