# cinemachine_set_targets

Set the Follow and/or LookAt transform targets on a Virtual Camera in one call.

**Signature:** `CinemachineSetTargets(string vcamName = null, int instanceId = 0, string path = null, string followName = null, string lookAtName = null)`

**Returns:** `{ success }` or `{ error }`

**Notes:**
- Pass only the parameters you want to change; omitting `followName` leaves the existing Follow target unchanged, and similarly for `lookAtName`.
- This is the canonical way to set targets. Do NOT use `cinemachine_set_target`, `cinemachine_set_follow`, or `cinemachine_set_lookat` — those commands do not exist.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string vcamName = "My VCam";
        int instanceId = 0;
        string path = null;
        string followName = "Player";   // set to null to leave unchanged
        string lookAtName = "Player";   // set to null to leave unchanged

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        WorkflowManager.SnapshotObject(go);

        var vcam = CinemachineAdapter.GetVCam(go);
        if (CinemachineAdapter.VCamOrError(vcam) is object vcamErr) { result.SetResult(vcamErr); return; }

        Undo.RecordObject(vcam, "Set Targets");
        if (followName != null)
            CinemachineAdapter.SetFollow(vcam, GameObjectFinder.Find(followName)?.transform);
        if (lookAtName != null)
            CinemachineAdapter.SetLookAt(vcam, GameObjectFinder.Find(lookAtName)?.transform);

        EditorUtility.SetDirty(go);
        result.SetResult(new { success = true });
    }
}
```
