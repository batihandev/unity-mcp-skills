# cinemachine_create_sequencer

Create a Sequencer camera (CM3: `CinemachineSequencerCamera`) or BlendList camera (CM2: `CinemachineBlendListCamera`) that plays child cameras in order.

**Signature:** `CinemachineCreateSequencer(string name, bool loop = false)`

**Returns:** `{ success, gameObjectName, instanceId, type, loop }` or `{ error }`

**Notes:**
- The actual component type name is resolved via `CinemachineAdapter.SequencerTypeName` to handle CM2/CM3 differences automatically.
- Auto-adds `CinemachineBrain` to Main Camera if missing.
- After creation, add child camera instructions with `cinemachine_sequencer_add_instruction`.
- Configure loop behavior later with `cinemachine_configure_camera_manager`.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "My Sequencer";
        bool loop = false;

        var go = new GameObject(name);
        var type = CinemachineAdapter.FindCinemachineType(CinemachineAdapter.SequencerTypeName);
        if (type == null)
        {
            result.SetResult(new { error = "Could not find Sequencer type: " + CinemachineAdapter.SequencerTypeName });
            return;
        }

        var seq = go.AddComponent(type) as MonoBehaviour;
        CinemachineAdapter.SetSequencerLoop(seq, loop);

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
            type = CinemachineAdapter.SequencerTypeName,
            loop
        });
    }
}
```
