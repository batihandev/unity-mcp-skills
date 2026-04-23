# texture_find_assets

Search for Texture2D assets in the project using an AssetDatabase filter.

## Signature

```
texture_find_assets(filter?: string = "", limit?: int = 50)
  → { success, totalFound, showing, textures[{ path, name, width, height }] }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filter = "";   // e.g. "label:character" or "hero"
        int limit = 50;

        var guids = AssetDatabase.FindAssets("t:Texture2D " + filter);
        var textures = guids.Take(limit).Select(guid =>
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return new
            {
                path,
                name = tex != null ? tex.name : System.IO.Path.GetFileNameWithoutExtension(path),
                width  = tex != null ? tex.width  : 0,
                height = tex != null ? tex.height : 0
            };
        }).ToArray();

        { result.SetResult(new { success = true, totalFound = guids.Length, showing = textures.Length, textures }); return; }
    }
}
```

## Notes

- `totalFound` reflects the full AssetDatabase result count before the `limit` cap is applied.
- Use `texture_get_info` to inspect a specific texture's runtime memory size and format.
- Use `texture_find_by_size` to filter by pixel dimensions.
