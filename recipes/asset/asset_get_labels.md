# asset_get_labels

Get the labels currently attached to an asset.

**Signature:** `AssetGetLabels(string assetPath)`

**Returns:** `{ success, assetPath, labels: [string] }`

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

        var labels = AssetDatabase.GetLabels(asset);
        result.SetResult(new { success = true, assetPath, labels });
    }
}
```
