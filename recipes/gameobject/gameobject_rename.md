# gameobject_rename

Rename a GameObject.

**Signature:** `GameObjectRename(string name = null, int instanceId = 0, string path = null, string newName = null)`

**Returns:** `{ success, oldName, newName, instanceId, path }`

## Notes

- At least one identifier (`name`, `instanceId`, or `path`) is required to locate the object.
- `newName` is required.

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

        /* Original Logic:

            if (Validate.Required(newName, "newName") is object err) return err;

            var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (error != null) return error;

            var oldName = go.name;
            WorkflowManager.SnapshotObject(go);
            Undo.RecordObject(go, "Rename GameObject");
            go.name = newName;

            return new { 
                success = true, 
                oldName, 
                newName = go.name, 
                instanceId = go.GetInstanceID(),
                path = GameObjectFinder.GetPath(go)
            };
        */
    }
}
```
