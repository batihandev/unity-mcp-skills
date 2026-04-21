# xr_setup_interaction_manager

Adds or finds an XRInteractionManager in the scene. Returns `alreadyExists: true` if one is already present.

**Signature:** `XRSetupInteractionManager(name string = null)`

**Returns:** `{ success, alreadyExists, name, instanceId }`

**Notes:**
- When `name` is null the created object is named `"XR Interaction Manager"`.
- Scenes should normally have exactly one XRInteractionManager.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
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
                    var managerType = XRReflectionHelper.ResolveXRType("XRInteractionManager");
                    if (managerType == null)
                        { result.SetResult(new { error = "XRInteractionManager type not found." }); return; }

                    // Check if one already exists
                    var existing = XRReflectionHelper.FindFirstOfXRType("XRInteractionManager");
                    if (existing != null)
                        { result.SetResult(new
                        {
                            success = true,
                            alreadyExists = true,
                            name = existing.gameObject.name,
                            instanceId = existing.gameObject.GetInstanceID()
                        }); return; }

                    var go = new GameObject(name ?? "XR Interaction Manager");
                    go.AddComponent(managerType);

                    Undo.RegisterCreatedObjectUndo(go, "Create XRInteractionManager");
                    WorkflowManager.SnapshotObject(go, SnapshotType.Created);

                    { result.SetResult(new
                    {
                        success = true,
                        alreadyExists = false,
                        name = go.name,
                        instanceId = go.GetInstanceID()
                    }); return; }
        #endif
    }
}
```
