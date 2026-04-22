# cinemachine_target_group_add_member

Add or update a member in a `CinemachineTargetGroup`. If the member already exists it is removed and re-added with the new weight and radius (upsert behavior).

**Signature:** `CinemachineTargetGroupAddMember(string groupName = null, int groupInstanceId = 0, string groupPath = null, string targetName = null, int targetInstanceId = 0, string targetPath = null, float weight = 1f, float radius = 1f)`

**Returns:** `{ success, message }` or `{ error }`

**Notes:**
- `weight` controls how much influence this member has on the group's average position.
- `radius` is the bounding sphere radius used for framing calculations.
- Provide at least one of `groupName`/`groupInstanceId`/`groupPath` and one of `targetName`/`targetInstanceId`/`targetPath`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

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
        float weight = 1f;
        float radius = 1f;

        var (groupGo, groupErr) = GameObjectFinder.FindOrError(groupName, groupInstanceId, groupPath);
        if (groupErr != null) { result.SetResult(groupErr); return; }

        var group = groupGo.GetComponent<CinemachineTargetGroup>();
        if (group == null) { result.SetResult(new { error = "GameObject is not a CinemachineTargetGroup" }); return; }

        var (targetGo, targetErr) = GameObjectFinder.FindOrError(targetName, targetInstanceId, targetPath);
        if (targetErr != null) { result.SetResult(targetErr); return; }

        WorkflowManager.SnapshotObject(groupGo);
        Undo.RecordObject(group, "Add TargetGroup Member");
        group.RemoveMember(targetGo.transform);
        group.AddMember(targetGo.transform, weight, radius);

        result.SetResult(new { success = true, message = "Added " + targetGo.name + " to " + groupGo.name + " (W:" + weight + ", R:" + radius + ")" });
    }
}
```
