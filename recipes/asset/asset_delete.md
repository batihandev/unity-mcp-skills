# asset_delete

Delete an asset from the project.

**Signature:** `AssetDelete(string assetPath)`

**Returns:** `{ success, deleted }` — plus a `serverAvailability` notice if the asset touches the script domain.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/old_texture.png"; // Project-relative path to delete

        if (Validate.SafePath(assetPath, "assetPath", isDelete: true) is object err)
        {
            result.SetResult(err);
            return;
        }
        if (!File.Exists(assetPath) && !Directory.Exists(assetPath))
        {
            result.SetResult(new { error = $"Asset not found: {assetPath}" });
            return;
        }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        AssetDatabase.DeleteAsset(assetPath);

        var res = new Dictionary<string, object>
        {
            ["success"] = true,
            ["deleted"] = assetPath
        };

        if (ServerAvailabilityHelper.AffectsScriptDomain(assetPath))
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                res,
                $"Deleted script-domain asset: {assetPath}. Unity may briefly reload the script domain.",
                alwaysInclude: true);
        }
        else
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                res,
                $"Asset deletion completed: {assetPath}. Unity may still be refreshing assets.",
                alwaysInclude: false);
        }

        result.SetResult(res);
    }
}
```
