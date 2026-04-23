# asset_move_batch

Move multiple assets within the project in a single batched operation.

**Signature:** `AssetMoveBatch(string items)`

`items` — JSON string of an array of `{ sourcePath, destinationPath }` objects.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, from, to }] }`

The batch uses `AssetDatabase.StartAssetEditing()` / `StopAssetEditing()` for performance, then calls `AssetDatabase.Refresh()`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchAssetMoveItem
{
    public string sourcePath;
    public string destinationPath;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchAssetMoveItem { sourcePath = "Assets/Old/A.png", destinationPath = "Assets/New/A.png" },
            new _BatchAssetMoveItem { sourcePath = "Assets/Old/B.png", destinationPath = "Assets/New/B.png" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var item in items)
            {
                var target = item.sourcePath ?? item.destinationPath;
                if (Validate.SafePath(item.sourcePath, "sourcePath") is object srcErr)
                { results.Add(new { target, success = false, error = "Invalid sourcePath" }); failCount++; continue; }
                if (Validate.SafePath(item.destinationPath, "destinationPath") is object dstErr)
                { results.Add(new { target, success = false, error = "Invalid destinationPath" }); failCount++; continue; }

                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.sourcePath);
                if (asset != null) WorkflowManager.SnapshotObject(asset);

                string error = AssetDatabase.MoveAsset(item.sourcePath, item.destinationPath);
                if (!string.IsNullOrEmpty(error))
                { results.Add(new { target, success = false, error }); failCount++; continue; }

                results.Add(new { target, success = true, from = item.sourcePath, to = item.destinationPath });
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
