# cinemachine_set_priority

Set an explicit priority value on a Virtual Camera. Higher priority wins activation when multiple cameras are eligible.

**Signature:** `CinemachineSetPriority(string vcamName = null, int instanceId = 0, string path = null, int priority = 10)`

**Returns:** `{ success, name, priority }` or `{ error }`

**Notes:**
- CM3 uses `Priority.Value` internally; the adapter wraps this transparently.
- Default priority is `10`. Use `cinemachine_set_active` when you just want to force a specific camera active immediately.
- Lower-priority cameras remain as fallbacks; the Brain switches automatically.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

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
        int priority = 20;

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var vcam = CinemachineAdapter.GetVCam(go);
        if (CinemachineAdapter.VCamOrError(vcam) is object vcamErr) { result.SetResult(vcamErr); return; }

        WorkflowManager.SnapshotObject(go);
        Undo.RecordObject(vcam, "Set Priority");
        CinemachineAdapter.SetPriority(vcam, priority);
        EditorUtility.SetDirty(vcam);

        result.SetResult(new { success = true, name = go.name, priority });
    }
}
```
