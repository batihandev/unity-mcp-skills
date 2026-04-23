# asset_get_info

Get metadata about an asset: name, type, GUID, and labels.

**Signature:** `AssetGetInfo(string assetPath)`

**Returns:** `{ path, name, type, guid, labels }` — or `{ error }` if the asset is not found.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/player.png"; // Project-relative asset path

        var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (asset == null)
        {
            result.SetResult(new { error = $"Asset not found: {assetPath}" });
            return;
        }

        result.SetResult(new
        {
            path = assetPath,
            name = asset.name,
            type = asset.GetType().Name,
            guid = AssetDatabase.AssetPathToGUID(assetPath),
            labels = AssetDatabase.GetLabels(asset)
        });
    }
}
```
