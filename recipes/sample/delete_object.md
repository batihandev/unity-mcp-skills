# delete_object

Delete a GameObject by name.

**Signature:** `DeleteObject(string objectName)`

**Returns:** `{ success, deleted, message }`

## Notes

- Simplified alternative to `gameobject_delete` — prefer the full `gameobject` module for production work.
- Uses `GameObjectFinder.FindOrError` to locate the object; returns an error if not found.
- Deletion is registered with Undo.

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
        string objectName = "ObjectName"; // required

        var (obj, err) = GameObjectFinder.FindOrError(objectName);
        if (err != null) { result.SetResult(err); return; }
        WorkflowManager.SnapshotObject(obj);
        Undo.DestroyObjectImmediate(obj);
        { result.SetResult(new { success = true, deleted = objectName, message = $"Deleted {objectName}" }); return; }
    }
}
```
