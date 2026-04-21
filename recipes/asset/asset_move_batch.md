# asset_move_batch

Move multiple assets within the project in a single batched operation.

**Signature:** `AssetMoveBatch(string items)`

`items` — JSON string of an array of `{ sourcePath, destinationPath }` objects.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, from, to }] }`

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
        string items = "[{\"sourcePath\":\"Assets/Old/A.png\",\"destinationPath\":\"Assets/New/A.png\"},{\"sourcePath\":\"Assets/Old/B.png\",\"destinationPath\":\"Assets/New/B.png\"}]";

        result.SetResult(BatchExecutor.Execute<BatchMoveItem>(items, item =>
        {
            if (Validate.SafePath(item.sourcePath, "sourcePath") is object srcErr)
                throw new System.Exception(((dynamic)srcErr).error);
            if (Validate.SafePath(item.destinationPath, "destinationPath") is object dstErr)
                throw new System.Exception(((dynamic)dstErr).error);

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.sourcePath);
            if (asset != null) WorkflowManager.SnapshotObject(asset);

            string error = AssetDatabase.MoveAsset(item.sourcePath, item.destinationPath);
            if (!string.IsNullOrEmpty(error))
                throw new System.Exception(error);

            return new
            {
                target = item.sourcePath,
                success = true,
                from = item.sourcePath,
                to = item.destinationPath,
                serverAvailability =
                    (ServerAvailabilityHelper.AffectsScriptDomain(item.sourcePath) || ServerAvailabilityHelper.AffectsScriptDomain(item.destinationPath))
                        ? ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                            $"Moved script-domain asset: {item.sourcePath} -> {item.destinationPath}. Unity may briefly reload the script domain.",
                            alwaysInclude: true)
                        : ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                            $"Asset move completed: {item.destinationPath}. Unity may still be refreshing assets.",
                            alwaysInclude: false)
            };
        }, item => item.sourcePath ?? item.destinationPath,
        setup: () => AssetDatabase.StartAssetEditing(),
        teardown: () => { AssetDatabase.StopAssetEditing(); AssetDatabase.Refresh(); }));
    }

    private class BatchMoveItem { public string sourcePath; public string destinationPath; }
}
```
