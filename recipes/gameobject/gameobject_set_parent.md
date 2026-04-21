# gameobject_set_parent

Set the parent of a GameObject (or unparent to scene root).

**Signature:** `GameObjectSetParent(string childName = null, int childInstanceId = 0, string childPath = null, string parentName = null, int parentInstanceId = 0, string parentPath = null)`

**Returns:** `{ success, child, parent, newPath }`

## Notes

- At least one child identifier (`childName`, `childInstanceId`, or `childPath`) is required.
- To unparent to scene root, omit all parent identifiers (or pass `parentName = ""`).
- `parent` in the return value is the parent name, or `"(root)"` when unparented.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Reparent child under a new parent
        string childName = "ChildObject";
        int childInstanceId = 0;
        string childPath = null;

        string parentName = "ParentObject";
        int parentInstanceId = 0;
        string parentPath = null;

        // To unparent: leave all parent params at default (null / 0)

        /* Original Logic:

            var (child, childError) = GameObjectFinder.FindOrError(childName, childInstanceId, childPath);
            if (childError != null) return childError;

            Transform parent = null;
            if (!string.IsNullOrEmpty(parentName) || parentInstanceId != 0 || !string.IsNullOrEmpty(parentPath))
            {
                var (parentGo, parentError) = GameObjectFinder.FindOrError(parentName, parentInstanceId, parentPath);
                if (parentError != null) return parentError;
                parent = parentGo.transform;
            }

            WorkflowManager.SnapshotObject(child.transform);
            Undo.SetTransformParent(child.transform, parent, "Set Parent");
            return new { 
                success = true, 
                child = child.name, 
                parent = parent?.name ?? "(root)",
                newPath = GameObjectFinder.GetPath(child)
            };
        */
    }
}
```
