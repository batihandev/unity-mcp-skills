# xr_add_simple_interactable

Adds XRSimpleInteractable for hover and select events without physical grab physics. Auto-adds a BoxCollider if none exists.

**Signature:** `XRAddSimpleInteractable(name string = null, instanceId int = 0, path string = null)`

**Returns:** `{ success, name, instanceId, interactableType, note }`

**Notes:**
- Does not add a Rigidbody — use `xr_add_grab_interactable` when physics grab is needed.
- Wire up callbacks with `xr_add_interaction_event` after adding this component.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;

        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) { result.SetResult(findErr); return; }

        Undo.RecordObject(go, "Add XRSimpleInteractable");

        var existing = go.GetComponent<XRSimpleInteractable>();
        var comp = existing != null ? existing : go.AddComponent<XRSimpleInteractable>();
        if (existing == null)
            Undo.RegisterCreatedObjectUndo(comp, "Add XRSimpleInteractable");

        // Ensure collider exists for interaction detection
        if (go.GetComponent<Collider>() == null)
            go.AddComponent<BoxCollider>();

        WorkflowManager.SnapshotObject(go);

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            interactableType = comp.GetType().Name,
            note = "Use xr_add_interaction_event to wire up hover/select callbacks."
        }); return; }
    }
}
```
