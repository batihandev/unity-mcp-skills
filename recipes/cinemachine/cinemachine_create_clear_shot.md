# cinemachine_create_clear_shot

Create a `CinemachineClearShot` camera that automatically selects the best-quality (clearest) child VCam.

**Signature:** `CinemachineCreateClearShot(string name)`

**Returns:** `{ success, name }` or `{ error }`

**Notes:**
- After creation, add child VCams as children of this GameObject in the hierarchy.
- Each child VCam should have a `CinemachineDeoccluder` / `CinemachineCollider` extension to evaluate shot quality.
- Configure activation timing and blend with `cinemachine_configure_camera_manager`.

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
        string name = "My Clear Shot";

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create Clear Shot");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        var cam = Undo.AddComponent<CinemachineClearShot>(go);
        if (cam == null)
        {
            result.SetResult(new { error = "Failed to add CinemachineClearShot component" });
            return;
        }

        result.SetResult(new { success = true, name = go.name });
    }
}
```
