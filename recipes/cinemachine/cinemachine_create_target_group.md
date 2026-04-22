# cinemachine_create_target_group

Create a new `CinemachineTargetGroup` GameObject in the scene. A target group lets a VCam track and frame multiple objects simultaneously.

**Signature:** `CinemachineCreateTargetGroup(string name)`

**Returns:** `{ success, name }` or `{ error }`

**Notes:**
- After creation, add members with `cinemachine_target_group_add_member`.
- Assign the resulting GameObject as a VCam's Follow or LookAt target via `cinemachine_set_targets`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "My Target Group";

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create TargetGroup");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        var group = Undo.AddComponent<CinemachineTargetGroup>(go);
        if (group == null)
        {
            result.SetResult(new { error = "Failed to add CinemachineTargetGroup component" });
            return;
        }

        result.SetResult(new { success = true, name = go.name });
    }
}
```
