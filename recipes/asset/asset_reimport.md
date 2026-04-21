# asset_reimport

Force reimport of a single asset using `ImportAssetOptions.ForceUpdate`.

**Signature:** `AssetReimport(string assetPath)`

**Returns:** `{ success, reimported }` — plus a `serverAvailability` notice if the asset touches the script domain.

**Note:** Accepts both project-relative paths (e.g. `Assets/foo.cs`) and absolute paths — the implementation resolves the full path when the project-relative form is not found on disk.

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
        string assetPath = "Assets/Scripts/MyScript.cs"; // Project-relative or absolute path

        if (string.IsNullOrEmpty(assetPath))
        {
            result.SetResult(new { success = false, error = "assetPath is required" });
            return;
        }
        if (Validate.SafePath(assetPath, "assetPath") is object pathErr)
        {
            result.SetResult(pathErr);
            return;
        }

        if (!File.Exists(assetPath) && !Directory.Exists(assetPath))
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var fullPath = System.IO.Path.Combine(projectRoot, assetPath);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                result.SetResult(new { success = false, error = $"Asset not found: {assetPath}" });
                return;
            }
        }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        var res = new Dictionary<string, object>
        {
            ["success"] = true,
            ["reimported"] = assetPath
        };

        if (ServerAvailabilityHelper.AffectsScriptDomain(assetPath))
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                res,
                $"Reimported script-domain asset: {assetPath}. Unity may briefly reload the script domain.",
                alwaysInclude: true);
        }
        else
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                res,
                $"Asset reimport completed: {assetPath}. Unity may still be refreshing assets.",
                alwaysInclude: false);
        }

        result.SetResult(res);
    }
}
```
