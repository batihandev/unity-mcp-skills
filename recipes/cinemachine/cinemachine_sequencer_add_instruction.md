# cinemachine_sequencer_add_instruction

Add a child camera instruction to a Sequencer/BlendList camera, defining how long to hold on that camera and how to blend into it.

**Signature:** `CinemachineSequencerAddInstruction(string sequencerName = null, int sequencerInstanceId = 0, string sequencerPath = null, string childCameraName = null, int childInstanceId = 0, string childPath = null, float hold = 2f, string blendStyle = "EaseInOut", float blendTime = 2f)`

**Returns:** `{ success, message }` or `{ error }`

**blendStyle values:** `"Cut"`, `"EaseInOut"`, `"EaseIn"`, `"EaseOut"`, `"HardIn"`, `"HardOut"`, `"Linear"`

**Notes:**
- Instructions are appended in order. Call this once per child camera.
- `hold` is the duration in seconds to stay on this child before advancing.
- `blendTime` is the blend duration entering this child camera.
- The child must have a `CinemachineVirtualCameraBase` component.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string sequencerName = "My Sequencer";
        int sequencerInstanceId = 0;
        string sequencerPath = null;
        string childCameraName = "VCam A";
        int childInstanceId = 0;
        string childPath = null;
        float hold = 3f;
        string blendStyle = "EaseInOut";
        float blendTime = 1f;

        var (go, err) = GameObjectFinder.FindOrError(sequencerName, sequencerInstanceId, sequencerPath);
        if (err != null) { result.SetResult(err); return; }

        var seq = go.GetComponent<CinemachineSequencerCamera>();
        if (seq == null) { result.SetResult(new { error = "Not a CinemachineSequencerCamera" }); return; }

        var (childGo, childErr) = GameObjectFinder.FindOrError(childCameraName, childInstanceId, childPath);
        if (childErr != null) { result.SetResult(childErr); return; }

        var childVcam = childGo.GetComponent<CinemachineVirtualCameraBase>();
        if (childVcam == null) { result.SetResult(new { error = "Child is not a Cinemachine Virtual Camera" }); return; }

        WorkflowManager.SnapshotObject(go);
        Undo.RecordObject(seq, "Add Sequencer Instruction");

        var blend = new CinemachineBlendDefinition { Time = blendTime };
        if (System.Enum.TryParse<CinemachineBlendDefinition.Styles>(blendStyle, true, out var parsed))
            blend.Style = parsed;

        if (seq.Instructions == null)
            seq.Instructions = new List<CinemachineSequencerCamera.Instruction>();
        seq.Instructions.Add(new CinemachineSequencerCamera.Instruction
        {
            Camera = childVcam,
            Hold = hold,
            Blend = blend
        });
        EditorUtility.SetDirty(seq);

        int count = seq.Instructions.Count;
        result.SetResult(new
        {
            success = true,
            message = "Added instruction #" + count + ": " + childGo.name + " (hold=" + hold + "s, blend=" + blendStyle + " " + blendTime + "s)"
        });
    }
}
```
