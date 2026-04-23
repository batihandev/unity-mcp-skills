# asset_import_batch

Import multiple external files into the project in a single batched operation.

**Signature:** `AssetImportBatch(string items)`

`items` — JSON string of an array of `{ sourcePath, destinationPath }` objects.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, sourcePath, destinationPath }] }`

The batch uses `AssetDatabase.StartAssetEditing()` / `StopAssetEditing()` for performance, then calls `AssetDatabase.Refresh()`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

internal sealed class _BatchAssetImportItem
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
            new _BatchAssetImportItem { sourcePath = "/abs/path/file1.png", destinationPath = "Assets/Textures/file1.png" },
            new _BatchAssetImportItem { sourcePath = "/abs/path/file2.png", destinationPath = "Assets/Textures/file2.png" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var item in items)
            {
                var target = item.destinationPath ?? item.sourcePath;
                if (Validate.SafePath(item.destinationPath, "destinationPath") is object dstErr)
                { results.Add(new { target, success = false, error = "Invalid destinationPath" }); failCount++; continue; }
                if (!File.Exists(item.sourcePath))
                { results.Add(new { target, success = false, error = "File not found: " + item.sourcePath }); failCount++; continue; }

                var dir = Path.GetDirectoryName(item.destinationPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                File.Copy(item.sourcePath, item.destinationPath, true);
                AssetDatabase.ImportAsset(item.destinationPath);

                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.destinationPath);
                if (asset != null) WorkflowManager.SnapshotCreatedAsset(asset);

                results.Add(new { target, success = true });
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
