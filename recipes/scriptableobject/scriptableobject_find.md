# scriptableobject_find

Find ScriptableObject assets by type name within a search path.

**Signature:** `ScriptableObjectFind(string typeName, string searchPath = "Assets", int limit = 50)`

**Returns:** `{ success, count, assets }`

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
