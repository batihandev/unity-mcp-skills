# asset_set_labels

Set labels on an asset. This overwrites all existing labels.

**Signature:** `AssetSetLabels(string assetPath, string labels)`

`labels` — comma-separated label string (e.g. `"ui,icon,hud"`). Empty entries are dropped automatically.

**Returns:** `{ success, assetPath, labels: [string] }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/player.png"; // Project-relative asset path
        string labels = "ui,icon,player";                // Comma-separated labels — overwrites all existing labels

        var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (asset == null)
        {
            result.SetResult(new { error = $"Asset not found: {assetPath}" });
            return;
        }

        var labelArray = labels.Split(',').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
        AssetDatabase.SetLabels(asset, labelArray);

        result.SetResult(new { success = true, assetPath, labels = labelArray });
    }
}
```
