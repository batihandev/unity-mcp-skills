# xr_configure_interactable

Fine-tunes properties on an existing XR interactable (XRGrabInteractable or XRSimpleInteractable). Pass only the fields you want to change.

**Signature:** `XRConfigureInteractable(name string = null, instanceId int = 0, path string = null, selectMode string = null, movementType string = null, throwOnDetach bool? = null, smoothPosition bool? = null, smoothRotation bool? = null, smoothPositionAmount float? = null, smoothRotationAmount float? = null, trackPosition bool? = null, trackRotation bool? = null)`

**Returns:** `{ success, name, instanceId, interactableType, changedProperties, selectModeOptions, movementTypeOptions }`

**Notes:**
- All parameters except the target selector are optional — omit unchanged fields.
- `movementType`, `smooth*`, `track*` apply to XRGrabInteractable only; XRSimpleInteractable supports `selectMode`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string selectMode = null;
        string movementType = null;
        bool? throwOnDetach = null;
        bool? smoothPosition = null;
        bool? smoothRotation = null;
        float? smoothPositionAmount = null;
        float? smoothRotationAmount = null;
        bool? trackPosition = null;
        bool? trackRotation = null;

        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) { result.SetResult(findErr); return; }

        // Find interactable — grab first, then simple, then any base.
        XRBaseInteractable baseComp = go.GetComponent<XRGrabInteractable>();
        if (baseComp == null) baseComp = go.GetComponent<XRSimpleInteractable>();
        if (baseComp == null) baseComp = go.GetComponent<XRBaseInteractable>();

        if (baseComp == null)
            { result.SetResult(new { error = $"No XR interactable found on '{go.name}'. Add one via xr_add_grab_interactable or xr_add_simple_interactable." }); return; }

        Undo.RecordObject(baseComp, "Configure XR Interactable");
        WorkflowManager.SnapshotObject(baseComp);

        var changed = new List<string>();

        if (!string.IsNullOrEmpty(selectMode) &&
            Enum.TryParse<InteractableSelectMode>(selectMode, true, out var sm))
        {
            baseComp.selectMode = sm;
            changed.Add("selectMode");
        }

        var grab = baseComp as XRGrabInteractable;
        if (grab != null)
        {
            if (!string.IsNullOrEmpty(movementType) &&
                Enum.TryParse<XRBaseInteractable.MovementType>(movementType, true, out var mt))
            {
                grab.movementType = mt;
                changed.Add("movementType");
            }
            if (throwOnDetach.HasValue)       { grab.throwOnDetach = throwOnDetach.Value; changed.Add("throwOnDetach"); }
            if (smoothPosition.HasValue)      { grab.smoothPosition = smoothPosition.Value; changed.Add("smoothPosition"); }
            if (smoothRotation.HasValue)      { grab.smoothRotation = smoothRotation.Value; changed.Add("smoothRotation"); }
            if (smoothPositionAmount.HasValue){ grab.smoothPositionAmount = smoothPositionAmount.Value; changed.Add("smoothPositionAmount"); }
            if (smoothRotationAmount.HasValue){ grab.smoothRotationAmount = smoothRotationAmount.Value; changed.Add("smoothRotationAmount"); }
            if (trackPosition.HasValue)       { grab.trackPosition = trackPosition.Value; changed.Add("trackPosition"); }
            if (trackRotation.HasValue)       { grab.trackRotation = trackRotation.Value; changed.Add("trackRotation"); }
        }

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            interactableType = baseComp.GetType().Name,
            changedProperties = changed,
            selectModeOptions = Enum.GetNames(typeof(InteractableSelectMode)),
            movementTypeOptions = Enum.GetNames(typeof(XRBaseInteractable.MovementType))
        }); return; }
    }
}
```
