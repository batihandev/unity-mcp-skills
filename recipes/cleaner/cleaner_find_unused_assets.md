# cleaner_find_unused_assets

Find potentially unused assets of a specific type by scanning dependencies across the project.

**Signature:** `CleanerFindUnusedAssets(string assetType = "Material", string searchPath = "Assets", int limit = 100)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `assetType` | string | No | "Material" | Asset type filter (e.g. Texture2D, AudioClip) |
| `searchPath` | string | No | "Assets" | Root path to search within |
| `limit` | int | No | 100 | Max number of results to return |

## Returns

```json
{
  "success": true,
  "assetType": "Material",
  "searchPath": "Assets",
  "potentiallyUnusedCount": 3,
  "note": "Assets may still be used via Resources.Load or Addressables",
  "assets": [
    { "path": "Assets/Mat.mat", "name": "Mat", "type": "Material", "sizeBytes": 2048 }
  ]
}
```

## Notes

- Assets inside `/Resources/` folders are excluded from results (they may be loaded at runtime via `Resources.Load`).
- Assets loaded via Addressables will also appear as unused — check before deleting.
- Use `cleaner_get_asset_usage` to confirm an individual asset before deletion.
- To delete found assets, use `cleaner_delete_assets` (two-step confirmation required).

**Prerequisites:** [`validate`](../_shared/validate.md)

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetType = "Material"; // e.g. Texture2D, AudioClip, Material
        string searchPath = "Assets";
        int limit = 100;

        if (Validate.SafePath(searchPath, "searchPath") is object pathErr) return;

        var filter = $"t:{assetType}";
        var guids = AssetDatabase.FindAssets(filter, new[] { searchPath });

        var candidatePaths = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .ToArray();
        var candidateSet = new HashSet<string>(candidatePaths, System.StringComparer.OrdinalIgnoreCase);
        var referencedCandidates = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var assetGuid in AssetDatabase.FindAssets("t:Object", new[] { searchPath }))
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (string.IsNullOrEmpty(assetPath) || assetPath.Contains("/Resources/"))
                continue;

            foreach (var dependency in AssetDatabase.GetDependencies(assetPath, true))
            {
                if (dependency == assetPath) continue;
                if (candidateSet.Contains(dependency))
                    referencedCandidates.Add(dependency);
            }
        }

        var potentiallyUnused = new List<object>();
        foreach (var path in candidatePaths)
        {
            if (potentiallyUnused.Count >= limit) break;
            if (path.Contains("/Resources/") || referencedCandidates.Contains(path))
                continue;

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            var fileInfo = new FileInfo(path);
            potentiallyUnused.Add(new
            {
                path,
                name = asset?.name,
                type = asset?.GetType().Name,
                sizeBytes = fileInfo.Exists ? fileInfo.Length : 0
            });
        }

        result.SetValue(new
        {
            success = true,
            assetType,
            searchPath,
            potentiallyUnusedCount = potentiallyUnused.Count,
            note = "Assets may still be used via Resources.Load or Addressables",
            assets = potentiallyUnused
        });
    }
}
```
