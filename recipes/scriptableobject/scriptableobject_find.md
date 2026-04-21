# scriptableobject_find

Find ScriptableObject assets by type name within a search path.

**Signature:** `ScriptableObjectFind(string typeName, string searchPath = "Assets", int limit = 50)`

**Returns:** `{ success, count, assets }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `typeName` | string | Yes | - | ScriptableObject type name to search for |
| `searchPath` | string | No | `"Assets"` | Folder path to search within |
| `limit` | int | No | `50` | Maximum number of results to return |

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string typeName = "ItemData"; // ScriptableObject type name to search for
        string searchPath = "Assets"; // Folder to search within
        int limit = 50; // Maximum results

        var guids = AssetDatabase.FindAssets($"t:{typeName}", new[] { searchPath });
        var results = guids.Take(limit).Select(g =>
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            return new { path = p, name = Path.GetFileNameWithoutExtension(p) };
        }).ToArray();

        result.SetResult(new { success = true, count = results.Length, assets = results });
    }
}
```
