# xr_setup_event_system

Finds or creates an EventSystem, removes `StandaloneInputModule` if present, and adds `XRUIInputModule` for XR-compatible UI input.

**Signature:** `XRSetupEventSystem()`

**Returns:** `{ success, name, instanceId, created, removedStandaloneInputModule, addedXRUIInputModule }`

**Notes:**
- Safe to call even if EventSystem already exists — it only modifies the input modules.
- Required before XR UI Canvas interactions work correctly.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `FindHelper.FindAll<T>(...)`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.SnapshotObject`

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.UI;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var eventSystems = FindHelper.FindAll<UnityEngine.EventSystems.EventSystem>();
        GameObject esGo;
        bool created = false;

        if (eventSystems.Length > 0)
        {
            esGo = eventSystems[0].gameObject;
        }
        else
        {
            esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            created = true;
        }

        Undo.RecordObject(esGo, "Setup XR EventSystem");

        var standalone = esGo.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        bool removedStandalone = false;
        if (standalone != null)
        {
            Undo.DestroyObjectImmediate(standalone);
            removedStandalone = true;
        }

        bool addedXRInput = false;
        var xrInput = esGo.GetComponent<XRUIInputModule>();
        if (xrInput == null)
        {
            esGo.AddComponent<XRUIInputModule>();
            addedXRInput = true;
        }

        if (created)
            Undo.RegisterCreatedObjectUndo(esGo, "Create XR EventSystem");

        WorkflowManager.SnapshotObject(esGo, created ? SnapshotType.Created : SnapshotType.Modified);

        result.SetResult(new
        {
            success = true,
            name = esGo.name,
            instanceId = esGo.GetInstanceID(),
            created,
            removedStandaloneInputModule = removedStandalone,
            addedXRUIInputModule = addedXRInput
        });
    }
}
```
