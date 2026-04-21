# gameobject_delete

Delete a GameObject.

**Signature:** `GameObjectDelete(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, deleted }` — `deleted` is the name of the object that was removed.

## Notes

- At least one identifier (`name`, `instanceId`, or `path`) is required.
- `instanceId` is preferred when precision matters (avoids ambiguity with duplicate names).
- The pre-deletion state is snapshotted for workflow tracking before the object is destroyed.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;       // provide at least one identifier
        int instanceId = 0;
        string path = null;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var deletedName = go.name;
        WorkflowManager.SnapshotObject(go); // Record pre-deletion state
        Undo.DestroyObjectImmediate(go);
        { result.SetResult(new { success = true, deleted = deletedName }); return; }
    }
}
```
