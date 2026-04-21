# cinemachine_set_active

Force a VCam to become the active camera by giving it a priority higher than all other VCams (SOLO behavior).

**Signature:** `CinemachineSetActive(string vcamName = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, message }` or `{ error }`

**Notes:**
- This sets `priority = maxExistingPriority + 1`. It does not revert other cameras.
- To revert, manually set the priority back with `cinemachine_set_priority`.
- In CM3, priority is accessed via `Priority.Value`; the adapter handles this transparently.

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

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        WorkflowManager.SnapshotObject(go);

        var vcam = CinemachineAdapter.GetVCam(go);
        if (CinemachineAdapter.VCamOrError(vcam) is object vcamErr) { result.SetResult(vcamErr); return; }

        int maxPrio = CinemachineAdapter.GetMaxPriority();
        CinemachineAdapter.SetPriority(vcam, maxPrio + 1);
        EditorUtility.SetDirty(vcam);

        result.SetResult(new { success = true, message = "Set Priority to " + CinemachineAdapter.GetPriority(vcam) + " (Highest)" });
    }
}
```
