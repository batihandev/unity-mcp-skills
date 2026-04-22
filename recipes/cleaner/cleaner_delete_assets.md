# cleaner_delete_assets

Delete specified assets using a mandatory two-step confirmation flow. Step 1 previews the deletion and returns a `confirmToken`. Step 2 executes the deletion using that token.

**Signature:** `CleanerDeleteAssets(string[] paths = null, string confirmToken = null)`

## Parameters

**Step 1 — Preview** (omit `confirmToken`):

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `paths` | string[] | Yes | Asset paths to delete |

**Step 2 — Execute** (provide `confirmToken`):

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `confirmToken` | string | Yes | Token returned by the preview call |

## Returns

**Step 1 — Preview response:**

```json
{
  "success": true,
  "action": "preview",
  "totalAssets": 3,
  "existingAssets": 3,
  "totalBytes": 307200,
  "totalMB": 0.293,
  "confirmToken": "a1b2c3d4",
  "message": "PREVIEW ONLY - 3 assets will be deleted (0.29 MB). To confirm, call again with confirmToken='a1b2c3d4'",
  "expiresIn": "5 minutes",
  "assetsToDelete": [
    { "path": "Assets/Old.mat", "exists": true, "sizeBytes": 102400, "sizeMB": 0.098 }
  ]
}
```

**Step 2 — Deleted response:**

```json
{
  "success": true,
  "action": "deleted",
  "deletedCount": 3,
  "totalMB": 0.293,
  "message": "Successfully deleted 3 assets",
  "results": [
    { "path": "Assets/Old.mat", "deleted": true }
  ]
}
```

## Notes

- Confirmation tokens expire after **5 minutes**. If expired, restart from Step 1.
- Invalid or expired tokens return `{ "success": false, "error": "..." }`.
- Each asset path is validated with `Validate.SafePath(..., isDelete: true)` before deletion.
- A workflow snapshot is taken for each asset before `AssetDatabase.MoveAssetToTrash` is called.
- `AssetDatabase.Refresh()` is called after all deletions complete.
- Use `cleaner_find_unused_assets`, `cleaner_find_duplicates`, or `cleaner_find_large_assets` to identify candidates first.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## C# Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Step 1: preview
        string[] paths = new[] { "Assets/OldMaterial.mat" };
        string confirmToken = null; // Set to token from Step 1 to execute

        // --- Step 2 (execute) branch ---
        if (!string.IsNullOrEmpty(confirmToken))
        {
            // Execute via skill: supply confirmToken returned from preview
            // cleaner_delete_assets(confirmToken: "<token>")
            return;
        }

        // --- Step 1 (preview) branch ---
        var previewResults = new List<object>();
        long totalBytes = 0;

        foreach (var path in paths)
        {
            var fileInfo = new FileInfo(path);
            var exists = File.Exists(path) || Directory.Exists(path);
            var size = fileInfo.Exists ? fileInfo.Length : 0;
            if (exists) totalBytes += size;

            previewResults.Add(new
            {
                path,
                exists,
                sizeBytes = size,
                sizeMB = size / (1024.0 * 1024.0)
            });
        }

        result.SetResult(new
        {
            success = true,
            mode = "preview",
            previewCount = previewResults.Count,
            totalBytes,
            totalMB = totalBytes / (1024.0 * 1024.0),
            assets = previewResults
        });
    }
}
```
