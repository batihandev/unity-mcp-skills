# asset_get_labels

Get the labels currently attached to an asset.

**Signature:** `AssetGetLabels(string assetPath)`

**Returns:** `{ success, assetPath, labels: [string] }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

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
