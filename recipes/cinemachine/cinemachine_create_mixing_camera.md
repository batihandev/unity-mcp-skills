# cinemachine_create_mixing_camera

Create a `CinemachineMixingCamera` that blends between its child VCams by weight.

**Signature:** `CinemachineCreateMixingCamera(string name)`

**Returns:** `{ success, name }` or `{ error }`

**Notes:**
- After creation, add child VCams as children of this GameObject and set their weights with `cinemachine_mixing_camera_set_weight`.

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
        string name = "My Mixing Camera";

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create Mixing Camera");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        var cam = Undo.AddComponent<CinemachineMixingCamera>(go);
        if (cam == null)
        {
            result.SetResult(new { error = "Failed to add CinemachineMixingCamera component" });
            return;
        }

        result.SetResult(new { success = true, name = go.name });
    }
}
```
