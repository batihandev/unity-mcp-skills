# uitk_find_files

Search for USS and/or UXML files in the project.

**Signature:** `UitkFindFiles(type string = "all", folder string = null, filter string = null, limit int = 200)`

**Returns:** `{ count, files[] { path, type, name } }`

**Notes:**
- `type` accepts `"uss"`, `"uxml"`, or `"all"`.
- `folder` restricts the search root (defaults to `"Assets"`).
- `filter` is a substring match against the asset path.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string type = "all";
        string folder = null;
        string filter = null;
        int limit = 200;

        var searchFolder = string.IsNullOrEmpty(folder) ? "Assets" : folder;
        var typeLower = type.ToLowerInvariant();
        var ussGuids  = (typeLower == "uxml") ? new string[0] : AssetDatabase.FindAssets("t:StyleSheet",      new[] { searchFolder });
        var uxmlGuids = (typeLower == "uss")  ? new string[0] : AssetDatabase.FindAssets("t:VisualTreeAsset", new[] { searchFolder });

        var seen = new System.Collections.Generic.HashSet<string>();
        var filteredPaths = new System.Collections.Generic.List<string>();

        foreach (var g in ussGuids.Concat(uxmlGuids))
        {
            if (filteredPaths.Count >= limit) break;
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (!seen.Add(p)) continue;
            var ext = Path.GetExtension(p).TrimStart('.').ToLowerInvariant();
            if (typeLower == "uss"  && ext != "uss")  continue;
            if (typeLower == "uxml" && ext != "uxml") continue;
            if (ext != "uss" && ext != "uxml") continue;
            if (!string.IsNullOrEmpty(filter) && !p.Contains(filter)) continue;
            filteredPaths.Add(p);
        }

        filteredPaths.Sort();
        var files = filteredPaths.Select(p => new
        {
            path = p,
            type = Path.GetExtension(p).TrimStart('.').ToLowerInvariant(),
            name = Path.GetFileNameWithoutExtension(p)
        }).ToArray();

        result.SetResult(new { count = files.Length, files });
    }
}
```
