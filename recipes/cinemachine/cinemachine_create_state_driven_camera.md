# cinemachine_create_state_driven_camera

Create a `CinemachineStateDrivenCamera` that activates child VCams based on Animator states.

**Signature:** `CinemachineCreateStateDrivenCamera(string name, string targetAnimatorName = null)`

**Returns:** `{ success, name }` or `{ error }`

**Notes:**
- The `AnimatedTarget` field on `CinemachineStateDrivenCamera` binds the Animator whose states drive activation.
- After creation, bind animation states to child cameras with `cinemachine_state_driven_camera_add_instruction`.
- You can also configure the Animator binding later via `cinemachine_configure_camera_manager`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "My State Driven Camera";
        string targetAnimatorName = "Player";  // set to null to skip

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create State Driven Camera");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        var cam = Undo.AddComponent<CinemachineStateDrivenCamera>(go);
        if (cam == null)
        {
            result.SetResult(new { error = "Failed to add CinemachineStateDrivenCamera component" });
            return;
        }

        if (!string.IsNullOrEmpty(targetAnimatorName))
        {
            var animatorGo = GameObjectFinder.Find(targetAnimatorName);
            if (animatorGo != null)
            {
                var animator = animatorGo.GetComponent<Animator>();
                if (animator != null)
                {
                    Undo.RecordObject(cam, "Set Animated Target");
                    cam.AnimatedTarget = animator;
                }
            }
        }

        result.SetResult(new { success = true, name = go.name });
    }
}
```
