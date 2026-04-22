# cinemachine_state_driven_camera_add_instruction

Add a state-to-camera mapping instruction to a `CinemachineStateDrivenCamera`. When the Animator enters the named state, the specified child VCam activates.

**Signature:** `CinemachineStateDrivenCameraAddInstruction(string cameraName = null, int cameraInstanceId = 0, string cameraPath = null, string stateName = null, string childCameraName = null, int childInstanceId = 0, string childPath = null, float minDuration = 0, float activateAfter = 0)`

**Returns:** `{ success, message }` or `{ error }`

**Notes:**
- `stateName` is hashed via `Animator.StringToHash` — it must exactly match an Animator state name.
- `minDuration` — minimum time the camera stays active once triggered.
- `activateAfter` — delay in seconds before the camera activates.
- The child camera must be a direct child of the StateDrivenCamera in the hierarchy.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string cameraName = "My State Driven Camera";
        int cameraInstanceId = 0;
        string cameraPath = null;
        string stateName = "Run";
        string childCameraName = "VCam_Run";
        int childInstanceId = 0;
        string childPath = null;
        float minDuration = 0f;
        float activateAfter = 0f;

        var (go, err) = GameObjectFinder.FindOrError(cameraName, cameraInstanceId, cameraPath);
        if (err != null) { result.SetResult(err); return; }

        var stateCam = go.GetComponent<CinemachineStateDrivenCamera>();
        if (stateCam == null) { result.SetResult(new { error = "Not a CinemachineStateDrivenCamera" }); return; }

        var (childGo, childErr) = GameObjectFinder.FindOrError(childCameraName, childInstanceId, childPath);
        if (childErr != null) { result.SetResult(childErr); return; }

        var childVcam = childGo.GetComponent<CinemachineVirtualCameraBase>();
        if (childVcam == null) { result.SetResult(new { error = "Child is not a Cinemachine Virtual Camera" }); return; }

        int hash = Animator.StringToHash(stateName);

        WorkflowManager.SnapshotObject(go);
        Undo.RecordObject(stateCam, "Add Instruction");

        var list = new List<CinemachineStateDrivenCamera.Instruction>();
        if (stateCam.Instructions != null) list.AddRange(stateCam.Instructions);
        list.Add(new CinemachineStateDrivenCamera.Instruction
        {
            FullHash = hash,
            Camera = childVcam,
            MinDuration = minDuration,
            ActivateAfter = activateAfter
        });
        stateCam.Instructions = list.ToArray();
        EditorUtility.SetDirty(stateCam);

        result.SetResult(new { success = true, message = "Added instruction: " + stateName + " -> " + childGo.name });
    }
}
```
