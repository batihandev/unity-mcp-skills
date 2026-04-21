# asset_reimport

Force reimport a single asset by path.

**Skill ID:** `asset_reimport`
**Source:** `AssetImportSkills.cs` — `AssetReimport`

## Signature

```
asset_reimport(assetPath: string) → { success, reimported }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path, e.g. `Assets/Textures/hero.png` |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/hero.png"; // Replace with target path

        if (string.IsNullOrEmpty(assetPath))
            return new { success = false, error = "assetPath is required" };
        if (Validate.SafePath(assetPath, "assetPath") is object pathErr) return pathErr;

        if (!File.Exists(assetPath) && !Directory.Exists(assetPath))
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var fullPath = Path.Combine(projectRoot, assetPath);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                return new { success = false, error = $"Asset not found: {assetPath}" };
        }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        var output = new Dictionary<string, object>
        {
            ["success"] = true,
            ["reimported"] = assetPath
        };

        if (ServerAvailabilityHelper.AffectsScriptDomain(assetPath))
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                output,
                $"Reimported script-domain asset: {assetPath}. Unity may briefly reload the script domain.",
                alwaysInclude: true);
        }
        else
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                output,
                $"Asset reimport completed: {assetPath}. Unity may still be refreshing assets.",
                alwaysInclude: false);
        }

        return output;
    }
}
```

## Notes

- Use after changing importer settings (texture, audio, model) to apply them.
- If the asset is in the script domain, Unity may briefly reload assemblies.
- For multiple assets, prefer `asset_reimport_batch`.
