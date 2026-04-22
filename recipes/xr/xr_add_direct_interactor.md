# xr_add_direct_interactor

Adds XRDirectInteractor for close-range hand grab and a trigger SphereCollider (if no collider is present) to a controller GameObject.

**Signature:** `XRAddDirectInteractor(name string = null, instanceId int = 0, path string = null, radius float = 0.1)`

**Returns:** `{ success, name, instanceId, interactorType, triggerRadius }`

**Notes:**
- The SphereCollider is automatically set to `isTrigger = true`.
- If a Collider already exists on the GameObject it is not replaced.
- Recommended radius range: `0.1–0.25`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

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
        float radius = 0.1f;

        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) { result.SetResult(findErr); return; }

        Undo.RecordObject(go, "Add XRDirectInteractor");

        var existing = go.GetComponent<XRDirectInteractor>();
        var comp = existing != null ? existing : go.AddComponent<XRDirectInteractor>();
        if (existing == null)
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
    }
}
```
