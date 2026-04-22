# asset_find

Find assets using an AssetDatabase search filter.

**Signature:** `AssetFind(string searchFilter, int limit = 50)`

**Returns:** `{ count, totalFound, assets: [{ path, name, type }] }`

`count` is the number of results returned (capped by `limit`); `totalFound` is the total number of GUIDs the database matched before the cap.

**Search filter syntax:**

| Filter | Example | Description |
|--------|---------|-------------|
| `t:Type` | `t:Texture2D` | By asset type |
| `l:Label` | `l:Architecture` | By label |
| `name` | `player` | By name (partial match) |
| Combined | `t:Material player` | Multiple filters |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string searchFilter = "t:Texture2D player"; // AssetDatabase search filter string
        int limit = 50;                              // Max results to return (default 50)

        var guids = AssetDatabase.FindAssets(searchFilter);
        var results = guids.Take(limit).Select(guid =>
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            return new
            {
                path,
                name = asset?.name,
                type = asset?.GetType().Name
            };
        }).ToArray();

        result.SetResult(new { count = results.Length, totalFound = guids.Length, assets = results });
    }
}
```
