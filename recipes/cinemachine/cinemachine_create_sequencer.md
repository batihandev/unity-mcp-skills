# cinemachine_create_sequencer

Create a `CinemachineSequencerCamera` that plays child cameras in order.

**Signature:** `CinemachineCreateSequencer(string name, bool loop = false)`

**Returns:** `{ success, gameObjectName, instanceId, type, loop }` or `{ error }`

**Notes:**
- Auto-adds `CinemachineBrain` to Main Camera if missing.
- After creation, add child camera instructions with `cinemachine_sequencer_add_instruction`.
- Configure loop behavior later with `cinemachine_configure_camera_manager`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "My Sequencer";
        bool loop = false;

        var go = new GameObject(name);
        var seq = go.AddComponent<CinemachineSequencerCamera>();
        seq.Loop = loop;

        var mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<CinemachineBrain>() == null)
            Undo.AddComponent<CinemachineBrain>(mainCamera.gameObject);

        Undo.RegisterCreatedObjectUndo(go, "Create Sequencer Camera");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new
        {
            success = true,
            gameObjectName = go.name,
            instanceId = go.GetInstanceID(),
            type = "CinemachineSequencerCamera",
            loop
        });
    }
}
```
