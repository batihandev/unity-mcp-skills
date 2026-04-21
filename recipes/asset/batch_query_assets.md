# batch_query_assets

Query multiple asset groups by type, label, or name filter in a single call.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `queries` | string | required | JSON array of query objects (see format below) |
| `limit` | int | 50 | Max results per query |

Query object format: `{ "filter": "t:Texture2D player", "label": "HUD" }` — `filter` uses AssetDatabase search syntax.

## C# Template

```csharp
using UnityEditor;
using System.Collections.Generic;

// queries: JSON string — e.g. '[{"filter":"t:Texture2D"},{"filter":"t:Material player"}]'
// limit: int — max results per query (default 50)

var queriesList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(queries ?? "[]");
int maxResults = limit > 0 ? limit : 50;

var groups = new List<object>();
foreach (var q in queriesList)
{
    string searchFilter = q.ContainsKey("filter") ? q["filter"] : "";
    string[] guids = AssetDatabase.FindAssets(searchFilter);
    var paths = new List<string>();
    foreach (var guid in guids)
    {
        if (paths.Count >= maxResults) break;
        paths.Add(AssetDatabase.GUIDToAssetPath(guid));
    }
    groups.Add(new { filter = searchFilter, count = paths.Count, assets = paths });
}

return new { success = true, totalGroups = groups.Count, groups };
```

## Notes

- `filter` uses standard AssetDatabase search syntax: `t:Type`, `l:Label`, name string, or combinations
- Results are capped at `limit` per query group; increase for large projects
- For a single-type search, prefer `asset_find` instead
