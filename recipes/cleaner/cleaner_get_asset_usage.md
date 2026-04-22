# cleaner_get_asset_usage

Find which assets in the project reference a specific asset (reverse dependency lookup).

**Signature:** `CleanerGetAssetUsage(string assetPath, int limit = 50)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `assetPath` | string | Yes | — | Project-relative path to the asset |
| `limit` | int | No | 50 | Max number of referencing assets to return |

## Returns

```json
{
  "success": true,
  "asset": {
    "path": "Assets/Materials/Rock.mat",
    "name": "Rock",
    "type": "Material"
  },
  "usedByCount": 2,
  "usedBy": [
    { "path": "Assets/Prefabs/Rock.prefab", "name": "Rock", "type": "GameObject" }
  ]
}
```

## Error Cases

```json
{ "success": false, "error": "Asset not found: Assets/Missing.mat" }
```

The path is also validated by `Validate.SafePath`; invalid paths return an error object before the file-existence check.

## Notes

- Uses `AssetDatabase.GetDependencies(path, false)` (non-recursive, direct references only) for each candidate asset.
- Scans all assets under `Assets/` regardless of the target asset's location.
- A `usedByCount` of 0 means no tracked asset directly references this path — but runtime loading via `Resources.Load` or Addressables will not be detected.
- Use this before `cleaner_delete_assets` to confirm an asset is truly safe to remove.

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
        string assetPath = "Assets/Materials/Rock.mat";
        int limit = 50;

        if (!File.Exists(assetPath))
        {
            Debug.LogError($"Asset not found: {assetPath}");
            return;
        }

        var usedBy = new List<object>();
        var allAssetGuids = AssetDatabase.FindAssets("t:Object", new[] { "Assets" });

        foreach (var guid in allAssetGuids)
        {
            if (usedBy.Count >= limit) break;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == assetPath) continue;

            var deps = AssetDatabase.GetDependencies(path, false);
            if (deps.Contains(assetPath))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                usedBy.Add(new
                {
                    path,
                    name = asset?.name,
                    type = asset?.GetType().Name
                });
            }
        }

        var targetAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);

        result.SetResult(new
        {
            success = true,
            asset = new
            {
                path = assetPath,
                name = targetAsset?.name,
                type = targetAsset?.GetType().Name
            },
            usedByCount = usedBy.Count,
            usedBy
        });
    }
}
```
