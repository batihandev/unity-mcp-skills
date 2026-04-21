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

                    Undo.RecordObject(go, "Add XRSocketInteractor");

                    var comp = XRReflectionHelper.AddXRComponent(go, "XRSocketInteractor");
                    if (comp == null)
                        { result.SetResult(new { error = "Failed to add XRSocketInteractor. Type not found in current XRI version." }); return; }

                    Undo.RegisterCreatedObjectUndo(comp, "Add XRSocketInteractor");

                    XRReflectionHelper.SetProperty(comp, "showInteractableHoverMeshes", showHoverMesh);
                    XRReflectionHelper.SetProperty(comp, "recycleDelayTime", recycleDelay);

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
        #endif
    }
}
```
