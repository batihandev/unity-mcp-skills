# cleaner_find_large_assets

Find the largest assets in the project by file size, sorted descending.

**Signature:** `CleanerFindLargeAssets(string searchPath = "Assets", int limit = 20, long minSizeBytes = 0)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `searchPath` | string | No | "Assets" | Root path to search within |
| `limit` | int | No | 20 | Max number of results to return |
| `minSizeBytes` | long | No | 0 | Minimum file size threshold (exclusive — files must be **strictly greater than** this value) |

## Returns

```json
{
  "success": true,
  "count": 5,
  "assets": [
    { "path": "Assets/Textures/Skybox.exr", "sizeBytes": 10485760, "sizeMB": 10.0 },
    { "path": "Assets/Audio/Music.ogg", "sizeBytes": 5242880, "sizeMB": 5.0 }
  ]
}
```

## Notes

- `.meta` files are excluded from results.
- `minSizeBytes` is an **exclusive** lower bound (`fileSize > minSizeBytes`). To find files 1 MB or larger, pass `minSizeBytes = 1048575`.
- Paths are normalized to use forward slashes and are relative from the `Assets/` segment.
- Results are sorted largest first and capped at `limit`.

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
        string searchPath = "Assets";
        int limit = 20;
        long minSizeBytes = 0; // exclusive — files must be strictly larger than this

        var files = Directory.GetFiles(searchPath, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".meta"))
            .Select(f => new FileInfo(f))
            .Where(fi => fi.Length > minSizeBytes)
            .OrderByDescending(fi => fi.Length)
            .Take(limit)
            .Select(fi =>
            {
                var relativePath = fi.FullName.Replace("\\", "/");
                var assetsIndex = relativePath.IndexOf("Assets/");
                if (assetsIndex >= 0) relativePath = relativePath.Substring(assetsIndex);
                return new { path = relativePath, sizeBytes = fi.Length, sizeMB = fi.Length / (1024.0 * 1024.0) };
            })
            .ToArray();

        result.SetResult(new { success = true, count = files.Length, assets = files });
    }
}
```
