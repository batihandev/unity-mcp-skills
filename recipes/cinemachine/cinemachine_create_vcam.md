# cinemachine_create_vcam

Create a new `CinemachineCamera` GameObject. Auto-adds `CinemachineBrain` to Main Camera if missing.

**Signature:** `CinemachineCreateVCam(string name, string folder = "Assets/Settings")`

**Returns:** `{ success, gameObjectName, instanceId }`

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
        string name = "My VCam";
        // folder param is accepted but not used for scene placement

        var go = new GameObject(name);
        var vcam = go.AddComponent<CinemachineCamera>();
        vcam.Priority.Value = 10;

        Undo.RegisterCreatedObjectUndo(go, "Create Virtual Camera");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            var brain = mainCamera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                var brainComp = Undo.AddComponent<CinemachineBrain>(mainCamera.gameObject);
                WorkflowManager.SnapshotCreatedComponent(brainComp);
            }
        }

        result.SetResult(new { success = true, gameObjectName = go.name, instanceId = go.GetInstanceID() });
    }
}
```
