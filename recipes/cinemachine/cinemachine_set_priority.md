# cinemachine_set_priority

Set an explicit priority value on a Virtual Camera. Higher priority wins activation when multiple cameras are eligible.

**Signature:** `CinemachineSetPriority(string vcamName = null, int instanceId = 0, string path = null, int priority = 10)`

**Returns:** `{ success, name, priority }` or `{ error }`

**Notes:**
- Priority is exposed as `CinemachineCamera.Priority.Value` in CM3.
- Default priority is `10`. Use `cinemachine_set_active` when you just want to force a specific camera active immediately.
- Lower-priority cameras remain as fallbacks; the Brain switches automatically.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.cinemachine` (≥ 3.1).

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
        int priority = 20;

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var vcam = go.GetComponent<CinemachineCamera>();
        if (vcam == null) { result.SetResult(new { error = "Not a CinemachineCamera" }); return; }

        WorkflowManager.SnapshotObject(go);
        Undo.RecordObject(vcam, "Set Priority");
        vcam.Priority.Value = priority;
        EditorUtility.SetDirty(vcam);

        result.SetResult(new { success = true, name = go.name, priority });
    }
}
```
