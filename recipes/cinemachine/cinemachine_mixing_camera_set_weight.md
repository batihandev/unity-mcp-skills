# cinemachine_mixing_camera_set_weight

Set the blend weight of a child VCam within a `CinemachineMixingCamera`.

**Signature:** `CinemachineMixingCameraSetWeight(string mixerName = null, int mixerInstanceId = 0, string mixerPath = null, string childName = null, int childInstanceId = 0, string childPath = null, float weight = 1f)`

**Returns:** `{ success, message }` or `{ error }`

**Notes:**
- `weight` range is typically 0.0 to 1.0 but any non-negative value is valid.
- A weight of `0` effectively disables that child's contribution.
- The child must be a direct child of the MixingCamera in the hierarchy and must have a `CinemachineVirtualCameraBase` component.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string mixerName = "My Mixing Camera";
        int mixerInstanceId = 0;
        string mixerPath = null;
        string childName = "VCam A";
        int childInstanceId = 0;
        string childPath = null;
        float weight = 0.7f;

        var (mixerGo, mixerErr) = GameObjectFinder.FindOrError(mixerName, mixerInstanceId, mixerPath);
        if (mixerErr != null) { result.SetResult(mixerErr); return; }

        var mixer = mixerGo.GetComponent<CinemachineMixingCamera>();
        if (mixer == null) { result.SetResult(new { error = "Not a CinemachineMixingCamera" }); return; }

        var (childGo, childErr) = GameObjectFinder.FindOrError(childName, childInstanceId, childPath);
        if (childErr != null) { result.SetResult(childErr); return; }

        var childVcam = childGo.GetComponent<CinemachineVirtualCameraBase>();
        if (childVcam == null) { result.SetResult(new { error = "Child is not a Cinemachine Virtual Camera" }); return; }

        WorkflowManager.SnapshotObject(mixerGo);
        Undo.RecordObject(mixer, "Set Mixing Weight");
        mixer.SetWeight(childVcam, weight);
        EditorUtility.SetDirty(mixer);

        result.SetResult(new { success = true, message = "Set weight of " + childGo.name + " to " + weight + " in " + mixerGo.name });
    }
}
```
