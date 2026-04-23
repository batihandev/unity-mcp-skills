# model_find_assets

Search for model assets in the project using an AssetDatabase filter.

## Signature

```
model_find_assets(filter?: string = "", limit?: int = 50)
  → { success, totalFound, showing, models[{ path, name }] }
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
        string filter = ""; // e.g. "label:character" or "hero"
        int limit = 50;

        var guids = AssetDatabase.FindAssets("t:Model " + filter);
        var models = guids.Take(limit).Select(guid =>
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return new { path, name = System.IO.Path.GetFileNameWithoutExtension(path) };
        }).ToArray();

        { result.SetResult(new { success = true, totalFound = guids.Length, showing = models.Length, models }); return; }
    }
}
```

## Notes

- `t:Model` matches FBX, OBJ, and other model formats recognised by Unity.
- `totalFound` reflects the full database count before the `limit` cap.
- Use `model_get_mesh_info` to inspect vertex/triangle stats on a specific model.
