# cinemachine_add_component

Add a Cinemachine component to a VCam by type name. **Legacy / CM2** — for CM3, prefer `cinemachine_set_component` which respects pipeline stages and removes conflicts.

**Signature:** `CinemachineAddComponent(string vcamName = null, int instanceId = 0, string path = null, string componentType = null)`

**Returns:** `{ success, message }` or `{ error }`

**Notes:**
- The type name is resolved via `CinemachineAdapter.FindCinemachineType`; the `"Cinemachine"` prefix is added automatically if omitted.
- This command does not remove existing components at the same pipeline stage. Use `cinemachine_set_component` on CM3 for proper stage management.

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
        string vcamName = "My VCam";
        int instanceId = 0;
        string path = null;
        string componentType = "CinemachineBasicMultiChannelPerlin";

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var type = CinemachineAdapter.FindCinemachineType(componentType);
        if (type == null) { result.SetResult(new { error = "Could not find Cinemachine component type: " + componentType }); return; }

        WorkflowManager.SnapshotObject(go);

        var comp = Undo.AddComponent(go, type);
        if (comp != null)
        {
            WorkflowManager.SnapshotCreatedComponent(comp);
            result.SetResult(new { success = true, message = "Added " + type.Name + " to " + go.name });
        }
        else
        {
            result.SetResult(new { error = "Failed to add component." });
        }
    }
}
```
