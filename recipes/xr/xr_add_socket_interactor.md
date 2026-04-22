# xr_add_socket_interactor

Adds XRSocketInteractor and a trigger SphereCollider (if no collider is present) to a GameObject for snap-to-slot object placement.

**Signature:** `XRAddSocketInteractor(name string = null, instanceId int = 0, path string = null, showHoverMesh bool = true, recycleDelay float = 1.0)`

**Returns:** `{ success, name, instanceId, interactorType, showHoverMesh, recycleDelay }`

**Notes:**
- Auto-added SphereCollider has `isTrigger = true` and `radius = 0.15`.
- `recycleDelay`: seconds before the socket can accept another object after release.
- `showHoverMesh` controls whether a ghost mesh appears when an interactable hovers over the socket.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        bool showHoverMesh = true;
        float recycleDelay = 1f;

        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) { result.SetResult(findErr); return; }

        Undo.RecordObject(go, "Add XRSocketInteractor");

        var existing = go.GetComponent<XRSocketInteractor>();
        var comp = existing != null ? existing : go.AddComponent<XRSocketInteractor>();
        if (existing == null)
            Undo.RegisterCreatedObjectUndo(comp, "Add XRSocketInteractor");

        comp.showInteractableHoverMeshes = showHoverMesh;
        comp.recycleDelayTime = recycleDelay;

        // Add SphereCollider trigger if no collider exists
        if (go.GetComponent<Collider>() == null)
        {
            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.15f;
        }

        WorkflowManager.SnapshotObject(go);

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            interactorType = comp.GetType().Name,
            showHoverMesh,
            recycleDelay
        }); return; }
    }
}
```
