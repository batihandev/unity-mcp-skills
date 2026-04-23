# xr_configure_haptics

Sets haptic feedback intensity and duration for select and hover events on an XR interactor.

**Signature:** `XRConfigureHaptics(name string = null, instanceId int = 0, path string = null, selectIntensity float = 0.5, selectDuration float = 0.1, hoverIntensity float = 0.1, hoverDuration float = 0.05)`

**Returns:** `{ success, name, instanceId, interactorType, changedProperties, selectIntensity, selectDuration, hoverIntensity, hoverDuration }`

**Notes:**
- Works on XRRayInteractor, XRDirectInteractor, XRSocketInteractor, or XRBaseInteractor.
- Haptic properties on XRI 3 live on `XRBaseInteractor` (e.g. `playHapticsOnSelectEntered`, `hapticSelectEnterIntensity`).

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        float selectIntensity = 0.5f;
        float selectDuration = 0.1f;
        float hoverIntensity = 0.1f;
        float hoverDuration = 0.05f;

        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) { result.SetResult(findErr); return; }

        // Find any interactor component — all inherit from XRBaseInteractor, which owns haptic props.
        Component comp = go.GetComponent<XRRayInteractor>();
        if (comp == null) comp = go.GetComponent<XRDirectInteractor>();
        if (comp == null) comp = go.GetComponent<XRSocketInteractor>();
        if (comp == null) comp = go.GetComponent<XRBaseInteractor>();

        if (comp == null)
            { result.SetResult(new { error = $"No XR interactor found on '{go.name}'." }); return; }

        Undo.RecordObject(comp, "Configure Haptics");
        WorkflowManager.SnapshotObject(comp);

        var changed = new List<string>();

        // Haptic properties live on XRRayInteractor / XRDirectInteractor / XRSocketInteractor.
        // XRBaseInteractor itself lacks them, so dispatch via the concrete type.
        if (comp is XRRayInteractor ray)
        {
            ray.playHapticsOnSelectEntered = true; changed.Add("playHapticsOnSelectEntered");
            ray.hapticSelectEnterIntensity = selectIntensity; changed.Add("hapticSelectEnterIntensity");
            ray.hapticSelectEnterDuration = selectDuration; changed.Add("hapticSelectEnterDuration");
            ray.playHapticsOnHoverEntered = hoverIntensity > 0; changed.Add("playHapticsOnHoverEntered");
            ray.hapticHoverEnterIntensity = hoverIntensity; changed.Add("hapticHoverEnterIntensity");
            ray.hapticHoverEnterDuration = hoverDuration; changed.Add("hapticHoverEnterDuration");
        }
        else if (comp is XRDirectInteractor dir)
        {
            dir.playHapticsOnSelectEntered = true; changed.Add("playHapticsOnSelectEntered");
            dir.hapticSelectEnterIntensity = selectIntensity; changed.Add("hapticSelectEnterIntensity");
            dir.hapticSelectEnterDuration = selectDuration; changed.Add("hapticSelectEnterDuration");
            dir.playHapticsOnHoverEntered = hoverIntensity > 0; changed.Add("playHapticsOnHoverEntered");
            dir.hapticHoverEnterIntensity = hoverIntensity; changed.Add("hapticHoverEnterIntensity");
            dir.hapticHoverEnterDuration = hoverDuration; changed.Add("hapticHoverEnterDuration");
        }

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
            hoverDuration
        }); return; }
    }
}
```
