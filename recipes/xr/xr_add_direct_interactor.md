# xr_add_direct_interactor

Adds XRDirectInteractor for close-range hand grab and a trigger SphereCollider (if no collider is present) to a controller GameObject.

**Signature:** `XRAddDirectInteractor(name string = null, instanceId int = 0, path string = null, radius float = 0.1)`

**Returns:** `{ success, name, instanceId, interactorType, triggerRadius }`

**Notes:**
- The SphereCollider is automatically set to `isTrigger = true`.
- If a Collider already exists on the GameObject it is not replaced.
- Recommended radius range: `0.1–0.25`.

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

                    Undo.RecordObject(go, "Add XRDirectInteractor");

                    // Add XRDirectInteractor
                    var comp = XRReflectionHelper.AddXRComponent(go, "XRDirectInteractor");
                    if (comp == null)
                        { result.SetResult(new { error = "Failed to add XRDirectInteractor. Type not found in current XRI version." }); return; }

                    Undo.RegisterCreatedObjectUndo(comp, "Add XRDirectInteractor");

                    // Add SphereCollider trigger if no collider exists
                    var collider = go.GetComponent<Collider>();
                    if (collider == null)
                    {
                        var sphere = go.AddComponent<SphereCollider>();
                        sphere.isTrigger = true;
                        sphere.radius = radius;
                    }

                    WorkflowManager.SnapshotObject(go);

                    { result.SetResult(new
                    {
                        success = true,
                        name = go.name,
                        instanceId = go.GetInstanceID(),
                        interactorType = comp.GetType().Name,
                        triggerRadius = radius
                    }); return; }
        #endif
    }
}
```
