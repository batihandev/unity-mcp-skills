# cinemachine_set_active

Force a VCam to become the active camera by giving it a priority higher than all other VCams (SOLO behavior).

**Signature:** `CinemachineSetActive(string vcamName = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, message }` or `{ error }`

**Notes:**
- This sets `Priority.Value = maxExistingPriority + 1`. It does not revert other cameras.
- To revert, manually set the priority back with `cinemachine_set_priority`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

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

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        WorkflowManager.SnapshotObject(go);

        var vcam = go.GetComponent<CinemachineCamera>();
        if (vcam == null) { result.SetResult(new { error = "Not a CinemachineCamera" }); return; }

        int maxPrio = 0;
        foreach (var other in FindHelper.FindAll<CinemachineCamera>())
        {
            int p = other.Priority.Value;
            if (p > maxPrio) maxPrio = p;
        }

        vcam.Priority.Value = maxPrio + 1;
        EditorUtility.SetDirty(vcam);

        result.SetResult(new { success = true, message = "Set Priority to " + vcam.Priority.Value + " (Highest)" });
    }
}
```
