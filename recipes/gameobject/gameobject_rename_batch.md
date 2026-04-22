# gameobject_rename_batch

Rename multiple GameObjects in one call.

**Signature:** `GameObjectRenameBatch(string items)`

`items`: JSON array of objects `{ name, instanceId, path, newName }`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, oldName, newName, instanceId }] }`

## Notes

- Each item requires at least one identifier (`name`, `instanceId`, or `path`) and a `newName`.
- A missing object or missing `newName` causes that item to fail without stopping the rest.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchRenameItem
{
    public string name;
    public int instanceId;
    public string path;
    public string newName;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchRenameItem { name = "Cube", newName = "Ground" },
            new _BatchRenameItem { instanceId = 12345, newName = "Player" },
            new _BatchRenameItem { path = "Parent/OldChild", newName = "NewChild" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var target = item.name ?? item.path ?? ("#" + item.instanceId);
            if (string.IsNullOrEmpty(item.newName)) { results.Add(new { target, success = false, error = "newName is required" }); failCount++; continue; }

            var (go, err) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (err != null) { results.Add(new { target, success = false, error = "Object not found" }); failCount++; continue; }

            var oldName = go.name;
            WorkflowManager.SnapshotObject(go);
            Undo.RecordObject(go, "Batch Rename " + go.name);
            go.name = item.newName;

            results.Add(new { success = true, oldName, newName = go.name, instanceId = go.GetInstanceID() });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
