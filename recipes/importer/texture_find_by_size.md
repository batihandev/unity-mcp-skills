# texture_find_by_size

Find textures whose largest dimension falls within a pixel range.

## Signature

```
texture_find_by_size(minSize?: int = 0, maxSize?: int = 99999, limit?: int = 50)
  → { success, count, textures[{ path, name, width, height }] }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int minSize = 0;
        int maxSize = 99999;
        int limit = 50;

        var guids = AssetDatabase.FindAssets("t:Texture2D");
        var results = new List<object>();

        foreach (var guid in guids)
        {
            if (results.Count >= limit) break;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;
            int maxDim = Mathf.Max(tex.width, tex.height);
            if (maxDim >= minSize && maxDim <= maxSize)
                results.Add(new { path, name = tex.name, width = tex.width, height = tex.height });
        }

        { result.SetResult(new { success = true, count = results.Count, textures = results }); return; }
    }
}
```

## Notes

- Filtering is done on the largest dimension (`max(width, height)`).
- Useful for finding oversized textures before a build (e.g. `minSize=2049` to find anything over 2K).
- `limit` is applied after the dimension filter, so the scan may load many textures; keep ranges tight on large projects.
