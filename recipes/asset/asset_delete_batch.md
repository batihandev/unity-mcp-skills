# asset_delete_batch

Delete multiple assets from the project in a single batched operation.

**Signature:** `AssetDeleteBatch(string items)`

`items` — JSON string of an array of `{ path }` objects.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, path }] }`

The batch uses `AssetDatabase.StartAssetEditing()` / `StopAssetEditing()` for performance, then calls `AssetDatabase.Refresh()`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // items must be a JSON string — not a native array
        string items = "[{\"path\":\"Assets/Textures/old1.png\"},{\"path\":\"Assets/Textures/old2.png\"}]";

        result.SetResult(BatchExecutor.Execute<BatchDeleteItem>(items, item =>
        {
            if (Validate.SafePath(item.path, "path", isDelete: true) is object pathErr)
                throw new System.Exception(((dynamic)pathErr).error);

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.path);
            if (asset != null) WorkflowManager.SnapshotObject(asset);
            if (!AssetDatabase.DeleteAsset(item.path))
                throw new System.Exception("Delete failed");

            return new
            {
                target = item.path,
                success = true,
                serverAvailability = ServerAvailabilityHelper.AffectsScriptDomain(item.path)
                    ? ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                        $"Deleted script-domain asset: {item.path}. Unity may briefly reload the script domain.",
                        alwaysInclude: true)
                    : ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                        $"Asset deletion completed: {item.path}. Unity may still be refreshing assets.",
                        alwaysInclude: false)
            };
        }, item => item.path,
        setup: () => AssetDatabase.StartAssetEditing(),
        teardown: () => { AssetDatabase.StopAssetEditing(); AssetDatabase.Refresh(); }));
    }

    private class BatchDeleteItem { public string path; }
}
```
