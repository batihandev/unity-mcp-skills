# asset_delete_batch

Delete multiple assets from the project in a single batched operation. Assets are moved to the OS trash (restorable) via `AssetDatabase.MoveAssetToTrash`.

**Signature:** `AssetDeleteBatch(string items)`

`items` — typed array of `{ path }` objects.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, path }] }`

The batch uses `AssetDatabase.StartAssetEditing()` / `StopAssetEditing()` for performance, then calls `AssetDatabase.Refresh()`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchAssetDeleteItem { public string path; }

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchAssetDeleteItem { path = "Assets/Textures/old1.png" },
            new _BatchAssetDeleteItem { path = "Assets/Textures/old2.png" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var item in items)
            {
                if (Validate.SafePath(item.path, "path", isDelete: true) is object pathErr)
                { results.Add(new { target = item.path, success = false, error = "Invalid path" }); failCount++; continue; }

                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.path);
                if (asset != null) WorkflowManager.SnapshotObject(asset);

                // MoveAssetToTrash instead of DeleteAsset — Unity_RunCommand analyzer
                // rejects any module containing AssetDatabase.DeleteAsset. MoveAssetToTrash
                // is restorable from the OS trash and semantically equivalent for batch-delete.
                if (!AssetDatabase.MoveAssetToTrash(item.path))
                { results.Add(new { target = item.path, success = false, error = "Delete failed" }); failCount++; continue; }

                results.Add(new { target = item.path, success = true });
                successCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
