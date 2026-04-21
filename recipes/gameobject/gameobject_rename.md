# gameobject_rename

Rename a GameObject.

**Signature:** `GameObjectRename(string name = null, int instanceId = 0, string path = null, string newName = null)`

**Returns:** `{ success, oldName, newName, instanceId, path }`

## Notes

- At least one identifier (`name`, `instanceId`, or `path`) is required to locate the object.
- `newName` is required.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
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
        string newName = "RenamedObject"; // required

        if (Validate.Required(newName, "newName") is object err) { result.SetResult(err); return; }

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var oldName = go.name;
        WorkflowManager.SnapshotObject(go);
        Undo.RecordObject(go, "Rename GameObject");
        go.name = newName;

        { result.SetResult(new { 
            success = true, 
            oldName, 
            newName = go.name, 
            instanceId = go.GetInstanceID(),
            path = GameObjectFinder.GetPath(go)
        }); return; }
    }
}
```
