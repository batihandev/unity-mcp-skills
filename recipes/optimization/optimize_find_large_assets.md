# optimize_find_large_assets

Find project assets whose on-disk size meets or exceeds a threshold. Optionally restrict to a specific asset type.

**Signature:** `OptimizeFindLargeAssets(int thresholdKB = 1024, string assetType = "", int limit = 50)`

**Returns:** `{ success, threshold, count, assets }`

- `threshold` — formatted string e.g. `"1024KB"`
- `assets` — array of `{ path, sizeKB, name }`, ordered by discovery (not by size)
- Only paths under `Assets/` are examined; meta files and non-file entries are skipped.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int thresholdKB = 1024;   // Minimum file size to report (inclusive)
        string assetType = "";    // e.g. "Texture2D", "AudioClip" — empty = all types
        int limit = 50;           // Maximum results to return

        var filter = string.IsNullOrEmpty(assetType) ? "" : $"t:{assetType}";
        var guids = AssetDatabase.FindAssets(filter);
        var large = new List<object>();

        foreach (var guid in guids)
        {
            if (large.Count >= limit) break;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/")) continue;

            var fullPath = System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(), path);
            if (!System.IO.File.Exists(fullPath)) continue;

            var sizeKB = (int)(new System.IO.FileInfo(fullPath).Length / 1024);
            if (sizeKB >= thresholdKB)
                large.Add(new { path, sizeKB, name = System.IO.Path.GetFileName(path) });
        }

        result.SetResult(new
        {
            success = true,
            threshold = $"{thresholdKB}KB",
            count = large.Count,
            assets = large
        });
    }
}
```
