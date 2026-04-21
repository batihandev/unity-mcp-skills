# cleaner_find_duplicates

Find duplicate files by grouping on file size first, then computing MD5 hashes to confirm true duplicates.

**Signature:** `CleanerFindDuplicates(string assetType = "Texture2D", string searchPath = "Assets", int limit = 50)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `assetType` | string | No | "Texture2D" | Asset type to scan |
| `searchPath` | string | No | "Assets" | Root path to search within |
| `limit` | int | No | 50 | Max number of duplicate groups to return |

## Returns

```json
{
  "success": true,
  "assetType": "Texture2D",
  "duplicateGroupCount": 2,
  "totalWastedBytes": 204800,
  "totalWastedMB": 0.195,
  "groups": [
    {
      "count": 2,
      "sizeBytes": 102400,
      "wastedBytes": 102400,
      "files": ["Assets/Tex1.png", "Assets/Tex2.png"]
    }
  ]
}
```

## Notes

- Files are first grouped by byte size (fast), then by MD5 hash (accurate). Only same-size files are hashed.
- `wastedBytes` = `sizeBytes * (count - 1)` — the space freed by keeping one copy.
- The `limit` caps the number of duplicate groups, not individual files.
- After identifying duplicates, use `cleaner_get_asset_usage` to decide which copy to keep before calling `cleaner_delete_assets`.

## C# Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Security.Cryptography;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetType = "Texture2D";
        string searchPath = "Assets";
        int limit = 50;

        var filter = $"t:{assetType}";
        var guids = AssetDatabase.FindAssets(filter, new[] { searchPath });

        // Group by file size first (fast filter)
        var sizeGroups = new Dictionary<long, List<string>>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists) continue;

            var size = fileInfo.Length;
            if (!sizeGroups.ContainsKey(size))
                sizeGroups[size] = new List<string>();
            sizeGroups[size].Add(path);
        }

        var duplicateGroups = new List<object>();
        using (var md5 = MD5.Create())
        {
            foreach (var group in sizeGroups.Values.Where(g => g.Count > 1))
            {
                if (duplicateGroups.Count >= limit) break;

                var hashGroups = new Dictionary<string, List<string>>();
                foreach (var path in group)
                {
                    try
                    {
                        using (var stream = File.OpenRead(path))
                        {
                            var hash = System.BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");
                            if (!hashGroups.ContainsKey(hash))
                                hashGroups[hash] = new List<string>();
                            hashGroups[hash].Add(path);
                        }
                    }
                    catch (System.Exception ex) { Debug.LogWarning($"Hash failed for {path}: {ex.Message}"); }
                }

                foreach (var hashGroup in hashGroups.Values.Where(g => g.Count > 1))
                {
                    var fileInfo = new FileInfo(hashGroup[0]);
                    duplicateGroups.Add(new
                    {
                        count = hashGroup.Count,
                        sizeBytes = fileInfo.Length,
                        wastedBytes = fileInfo.Length * (hashGroup.Count - 1),
                        files = hashGroup
                    });
                }
            }
        }

        var totalWasted = duplicateGroups.Sum(d => (long)((dynamic)d).wastedBytes);

        result.SetValue(new
        {
            success = true,
            assetType,
            duplicateGroupCount = duplicateGroups.Count,
            totalWastedBytes = totalWasted,
            totalWastedMB = totalWasted / (1024.0 * 1024.0),
            groups = duplicateGroups
        });
    }
}
```
