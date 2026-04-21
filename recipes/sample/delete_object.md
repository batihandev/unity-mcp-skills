# delete_object

Delete a GameObject by name.

**Signature:** `DeleteObject(string objectName)`

**Returns:** `{ success, deleted, message }`

## Notes

- Simplified alternative to `gameobject_delete` — prefer the full `gameobject` module for production work.
- Uses `GameObjectFinder.FindOrError` to locate the object; returns an error if not found.
- Deletion is registered with Undo.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string objectName = "ObjectName"; // required

        /* Original Logic:

            var (obj, err) = GameObjectFinder.FindOrError(objectName);
            if (err != null) return err;
            WorkflowManager.SnapshotObject(obj);
            Undo.DestroyObjectImmediate(obj);
            return new { success = true, deleted = objectName, message = $"Deleted {objectName}" };
        */
    }
}
```
