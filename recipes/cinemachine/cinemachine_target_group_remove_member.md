# cinemachine_target_group_remove_member

Remove a member from a `CinemachineTargetGroup`.

**Signature:** `CinemachineTargetGroupRemoveMember(string groupName = null, int groupInstanceId = 0, string groupPath = null, string targetName = null, int targetInstanceId = 0, string targetPath = null)`

**Returns:** `{ success, message }` or `{ error }`

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
        string groupName = "My Target Group";
        int groupInstanceId = 0;
        string groupPath = null;
        string targetName = "Player";
        int targetInstanceId = 0;
        string targetPath = null;

        var (groupGo, groupErr) = GameObjectFinder.FindOrError(groupName, groupInstanceId, groupPath);
        if (groupErr != null) { result.SetResult(groupErr); return; }

        var group = groupGo.GetComponent<CinemachineTargetGroup>();
        if (group == null) { result.SetResult(new { error = "GameObject is not a CinemachineTargetGroup" }); return; }

        var (targetGo, targetErr) = GameObjectFinder.FindOrError(targetName, targetInstanceId, targetPath);
        if (targetErr != null) { result.SetResult(targetErr); return; }

        WorkflowManager.SnapshotObject(groupGo);
        Undo.RecordObject(group, "Remove TargetGroup Member");
        group.RemoveMember(targetGo.transform);

        result.SetResult(new { success = true, message = "Removed " + targetGo.name + " from " + groupGo.name });
    }
}
```
