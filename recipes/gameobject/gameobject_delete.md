# gameobject_delete

Delete a GameObject.

**Signature:** `GameObjectDelete(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, deleted }` — `deleted` is the name of the object that was removed.

## Notes

- At least one identifier (`name`, `instanceId`, or `path`) is required.
- `instanceId` is preferred when precision matters (avoids ambiguity with duplicate names).
- The pre-deletion state is snapshotted for workflow tracking before the object is destroyed.

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

        /* Original Logic:

            var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (error != null) return error;

            var deletedName = go.name;
            WorkflowManager.SnapshotObject(go); // Record pre-deletion state
            Undo.DestroyObjectImmediate(go);
            return new { success = true, deleted = deletedName };
        */
    }
}
```
