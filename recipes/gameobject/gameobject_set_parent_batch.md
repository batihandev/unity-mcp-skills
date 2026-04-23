# gameobject_set_parent_batch

Set parent for multiple GameObjects in one call.

**Signature:** `GameObjectSetParentBatch(string items)`

`items`: JSON array of objects `{ childName, childInstanceId, childPath, parentName, parentInstanceId, parentPath }`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target, parent }] }`

## Notes

- At least one child identifier per item is required.
- Omit all parent fields to unparent to scene root.
- A missing child or parent causes that item to fail without stopping the rest.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchSetParentItem
{
    public string childName;
    public int childInstanceId;
    public string childPath;
    public string parentName;
    public int parentInstanceId;
    public string parentPath;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchSetParentItem { childName = "Wheel_FL", parentName = "Car" },
            new _BatchSetParentItem { childName = "Wheel_FR", parentName = "Car" },
            new _BatchSetParentItem { childInstanceId = 12345, parentPath = "Level/Root" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var target = item.childName ?? item.childPath ?? ("#" + item.childInstanceId);
            var (child, childErr) = GameObjectFinder.FindOrError(item.childName, item.childInstanceId, item.childPath);
            if (childErr != null) { results.Add(new { target, success = false, error = "Child object not found" }); failCount++; continue; }

            Transform parent = null;
            if (!string.IsNullOrEmpty(item.parentName) || item.parentInstanceId != 0 || !string.IsNullOrEmpty(item.parentPath))
            {
                var (parentGo, parentErr) = GameObjectFinder.FindOrError(item.parentName, item.parentInstanceId, item.parentPath);
                if (parentErr != null) { results.Add(new { target, success = false, error = "Parent not found" }); failCount++; continue; }
                parent = parentGo.transform;
            }

            WorkflowManager.SnapshotObject(child.transform);
            Undo.SetTransformParent(child.transform, parent, "Batch Set Parent");
            results.Add(new { target = child.name, success = true, parent = parent?.name ?? "(root)" });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
