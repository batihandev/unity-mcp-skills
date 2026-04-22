# gameobject_duplicate_batch

Duplicate multiple GameObjects in one call.

**Signature:** `GameObjectDuplicateBatch(string items)`

`items`: JSON array of objects `{ name, instanceId, path }`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, originalName, copyName, copyInstanceId, copyPath }] }`

## Notes

- Each copy is placed under the same parent as its original and named `<originalName>_Copy`.
- A missing source object causes that item to fail without stopping the rest.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchDuplicateItem
{
    public string name;
    public int instanceId;
    public string path;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchDuplicateItem { name = "Cube" },
            new _BatchDuplicateItem { instanceId = 12345 },
            new _BatchDuplicateItem { path = "Parent/ChildObject" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var target = item.name ?? item.path ?? ("#" + item.instanceId);
            var (go, err) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (err != null) { results.Add(new { target, success = false, error = "Object not found" }); failCount++; continue; }

            var copy = Object.Instantiate(go, go.transform.parent);
            copy.name = go.name + "_Copy";
            Undo.RegisterCreatedObjectUndo(copy, "Batch Duplicate " + go.name);
            WorkflowManager.SnapshotObject(copy, SnapshotType.Created);

            results.Add(new
            {
                success = true,
                originalName = go.name,
                copyName = copy.name,
                copyInstanceId = copy.GetInstanceID(),
                copyPath = GameObjectFinder.GetPath(copy)
            });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
