# gameobject_set_parent

Set the parent of a GameObject (or unparent to scene root).

**Signature:** `GameObjectSetParent(string childName = null, int childInstanceId = 0, string childPath = null, string parentName = null, int parentInstanceId = 0, string parentPath = null)`

**Returns:** `{ success, child, parent, newPath }`

## Notes

- At least one child identifier (`childName`, `childInstanceId`, or `childPath`) is required.
- To unparent to scene root, omit all parent identifiers (or pass `parentName = ""`).
- `parent` in the return value is the parent name, or `"(root)"` when unparented.

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
        // Reparent child under a new parent
        string childName = "ChildObject";
        int childInstanceId = 0;
        string childPath = null;

        string parentName = "ParentObject";
        int parentInstanceId = 0;
        string parentPath = null;

        // To unparent: leave all parent params at default (null / 0)

        var (child, childError) = GameObjectFinder.FindOrError(childName, childInstanceId, childPath);
        if (childError != null) { result.SetResult(childError); return; }

        Transform parent = null;
        if (!string.IsNullOrEmpty(parentName) || parentInstanceId != 0 || !string.IsNullOrEmpty(parentPath))
        {
            var (parentGo, parentError) = GameObjectFinder.FindOrError(parentName, parentInstanceId, parentPath);
            if (parentError != null) { result.SetResult(parentError); return; }
            parent = parentGo.transform;
        }

        WorkflowManager.SnapshotObject(child.transform);
        Undo.SetTransformParent(child.transform, parent, "Set Parent");
        { result.SetResult(new { 
            success = true, 
            child = child.name, 
            parent = parent?.name ?? "(root)",
            newPath = GameObjectFinder.GetPath(child)
        }); return; }
    }
}
```
