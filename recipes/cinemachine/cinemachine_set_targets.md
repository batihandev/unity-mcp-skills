# cinemachine_set_targets

Set the Follow and/or LookAt transform targets on a Virtual Camera in one call.

**Signature:** `CinemachineSetTargets(string vcamName = null, int instanceId = 0, string path = null, string followName = null, string lookAtName = null)`

**Returns:** `{ success }` or `{ error }`

**Notes:**
- Pass only the parameters you want to change; omitting `followName` leaves the existing Follow target unchanged, and similarly for `lookAtName`.
- This is the canonical way to set targets. Do NOT use `cinemachine_set_target`, `cinemachine_set_follow`, or `cinemachine_set_lookat` — those commands do not exist.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

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

        var vcam = go.GetComponent<CinemachineCamera>();
        if (vcam == null) { result.SetResult(new { error = "Not a CinemachineCamera" }); return; }

        Undo.RecordObject(vcam, "Set Targets");
        if (followName != null)
            vcam.Follow = GameObjectFinder.Find(followName)?.transform;
        if (lookAtName != null)
            vcam.LookAt = GameObjectFinder.Find(lookAtName)?.transform;

        EditorUtility.SetDirty(go);
        result.SetResult(new { success = true });
    }
}
```
