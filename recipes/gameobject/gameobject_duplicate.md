# gameobject_duplicate

Duplicate a GameObject. The copy is placed under the same parent and gets the suffix `_Copy`.

**Signature:** `GameObjectDuplicate(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, originalName, copyName, copyInstanceId, copyPath }`

## Notes

- At least one identifier (`name`, `instanceId`, or `path`) is required.
- The copy is named `<originalName>_Copy` and placed at the same hierarchy level as the original.

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

            var copy = Object.Instantiate(go, go.transform.parent);
            copy.name = go.name + "_Copy";
            Undo.RegisterCreatedObjectUndo(copy, "Duplicate " + go.name);
            WorkflowManager.SnapshotObject(copy, SnapshotType.Created);

            return new {
                success = true,
                originalName = go.name,
                copyName = copy.name,
                copyInstanceId = copy.GetInstanceID(),
                copyPath = GameObjectFinder.GetPath(copy)
            };
        */
    }
}
```
