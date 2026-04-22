# xr_setup_interaction_manager

Adds or finds an XRInteractionManager in the scene. Returns `alreadyExists: true` if one is already present.

**Signature:** `XRSetupInteractionManager(name string = null)`

**Returns:** `{ success, alreadyExists, name, instanceId }`

**Notes:**
- When `name` is null the created object is named `"XR Interaction Manager"`.
- Scenes should normally have exactly one XRInteractionManager.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;

        var existing = UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();
        if (existing != null)
            { result.SetResult(new
            {
                success = true,
                alreadyExists = true,
                name = existing.gameObject.name,
                instanceId = existing.gameObject.GetInstanceID()
            }); return; }

        var go = new GameObject(name ?? "XR Interaction Manager");
        go.AddComponent<XRInteractionManager>();

        Undo.RegisterCreatedObjectUndo(go, "Create XRInteractionManager");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        { result.SetResult(new
        {
            success = true,
            alreadyExists = false,
            name = go.name,
            instanceId = go.GetInstanceID()
        }); return; }
    }
}
```
