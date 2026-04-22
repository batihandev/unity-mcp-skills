# gameobject_delete_batch

Delete multiple GameObjects in one call.

**Signature:** `GameObjectDeleteBatch(string items)`

`items`: JSON array of **strings** (object names) or **objects** `{ name, instanceId, path }`. Both forms can be mixed in the same array.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target }] }`

## Notes

- Accepts a plain string array (`["Cube", "Sphere"]`) for convenience, in addition to the full object form.
- Each item is normalized internally; a missing object causes that item to fail without stopping the rest.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchDeleteItem
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
            new _BatchDeleteItem { name = "Cube" },
            new _BatchDeleteItem { instanceId = 12345 },
            new _BatchDeleteItem { path = "Parent/Child" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var target = item.name ?? item.path ?? ("#" + item.instanceId);
            var (go, err) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (err != null) { results.Add(new { target, success = false, error = "Object not found" }); failCount++; continue; }

            var deletedName = go.name;
            WorkflowManager.SnapshotObject(go);
            Undo.DestroyObjectImmediate(go);
            results.Add(new { target = deletedName, success = true });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
