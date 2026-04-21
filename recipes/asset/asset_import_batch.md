# asset_import_batch

Import multiple external files into the project in a single batched operation.

**Signature:** `AssetImportBatch(string items)`

`items` — JSON string of an array of `{ sourcePath, destinationPath }` objects.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, sourcePath, destinationPath }] }`

The batch uses `AssetDatabase.StartAssetEditing()` / `StopAssetEditing()` for performance, then calls `AssetDatabase.Refresh()`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // items must be a JSON string — not a native array
        string items = "[{\"sourcePath\":\"/abs/path/file1.png\",\"destinationPath\":\"Assets/Textures/file1.png\"},{\"sourcePath\":\"/abs/path/file2.png\",\"destinationPath\":\"Assets/Textures/file2.png\"}]";

        result.SetResult(BatchExecutor.Execute<BatchImportItem>(items, item =>
        {
            if (Validate.SafePath(item.destinationPath, "destinationPath") is object dstErr)
                throw new System.Exception(((dynamic)dstErr).error);
            if (!File.Exists(item.sourcePath))
                throw new System.Exception("File not found");

            var dir = Path.GetDirectoryName(item.destinationPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.Copy(item.sourcePath, item.destinationPath, true);
            AssetDatabase.ImportAsset(item.destinationPath);

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.destinationPath);
            if (asset != null) WorkflowManager.SnapshotCreatedAsset(asset);

            return new
            {
                target = item.destinationPath,
                success = true,
                serverAvailability = ServerAvailabilityHelper.AffectsScriptDomain(item.destinationPath)
                    ? ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                        $"Imported script-domain asset: {item.destinationPath}. Unity may briefly reload the script domain.",
                        alwaysInclude: true)
                    : ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                        $"Asset import completed: {item.destinationPath}. Unity may still be refreshing assets.",
                        alwaysInclude: false)
            };
        }, item => item.destinationPath ?? item.sourcePath,
        setup: () => AssetDatabase.StartAssetEditing(),
        teardown: () => { AssetDatabase.StopAssetEditing(); AssetDatabase.Refresh(); }));
    }

    private class BatchImportItem { public string sourcePath; public string destinationPath; }
}
```
