# validate_find_unused_assets

Find potentially unused assets of a given type by checking whether any other asset references them.

**Signature:** `ValidateFindUnusedAssets(assetType string = "Material", limit int = 100)`

**Returns:** `{ assetType, potentiallyUnusedCount, note, assets: [{ path, name, type }] }`

**Notes:**
- Results are candidates only; assets loaded via `Resources.Load` or referenced from scripts at runtime will still appear
- `assetType` accepts any Unity asset type string: `"Material"`, `"Texture2D"`, `"Prefab"`, `"AudioClip"`, etc.
- The dependency scan covers all assets under `Assets/`; large projects may be slow

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetType = "Material";
        int limit = 100;

        var filter = $"t:{assetType}";
        var guids = AssetDatabase.FindAssets(filter);
        var candidatePaths = new HashSet<string>(
            guids.Select(AssetDatabase.GUIDToAssetPath).Where(p => !string.IsNullOrEmpty(p)),
            System.StringComparer.OrdinalIgnoreCase);

        var allGuids = AssetDatabase.FindAssets("t:Object", new[] { "Assets" });
        var referencedPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var g in allGuids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(g);
            if (string.IsNullOrEmpty(assetPath)) continue;
            foreach (var dep in AssetDatabase.GetDependencies(assetPath, true))
            {
                if (dep != assetPath && candidatePaths.Contains(dep))
                    referencedPaths.Add(dep);
            }
        }

        var potentiallyUnused = new List<object>();
        foreach (var path in candidatePaths)
        {
            if (potentiallyUnused.Count >= limit) break;
            if (referencedPaths.Contains(path)) continue;

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            potentiallyUnused.Add(new
            {
                path,
                name = asset?.name,
                type = asset?.GetType().Name
            });
        }

        result.SetResult(new
        {
            assetType,
            potentiallyUnusedCount = potentiallyUnused.Count,
            note = "These assets may still be used via scripts or Resources.Load",
            assets = potentiallyUnused
        });
    }
}
```
