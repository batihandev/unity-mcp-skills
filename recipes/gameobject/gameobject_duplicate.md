# gameobject_duplicate

Duplicate a GameObject. The copy is placed under the same parent and gets the suffix `_Copy`.

**Signature:** `GameObjectDuplicate(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, originalName, copyName, copyInstanceId, copyPath }`

## Notes

- At least one identifier (`name`, `instanceId`, or `path`) is required.
- The copy is named `<originalName>_Copy` and placed at the same hierarchy level as the original.

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

        var copy = Object.Instantiate(go, go.transform.parent);
        copy.name = go.name + "_Copy";
        Undo.RegisterCreatedObjectUndo(copy, "Duplicate " + go.name);
        WorkflowManager.SnapshotObject(copy, SnapshotType.Created);

        { result.SetResult(new {
            success = true,
            originalName = go.name,
            copyName = copy.name,
            copyInstanceId = copy.GetInstanceID(),
            copyPath = GameObjectFinder.GetPath(copy)
        }); return; }
    }
}
```
