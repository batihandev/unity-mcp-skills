# asset_reimport_batch

Reimport multiple assets that match an AssetDatabase search filter.

**Skill ID:** `asset_reimport_batch`
**Source:** `AssetImportSkills.cs` — `AssetReimportBatch`

## Signature

```
asset_reimport_batch(searchFilter?: string = "*", folder?: string = "Assets", limit?: int = 100)
  → { success, count, assets }
```

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `searchFilter` | string | no | `"*"` | AssetDatabase search filter, e.g. `"t:Texture2D"` |
| `folder` | string | no | `"Assets"` | Root folder to search within |
| `limit` | int | no | `100` | Max number of assets to reimport |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string searchFilter = "t:Texture2D"; // Replace with desired filter
        string folder = "Assets";
        int limit = 100;

        if (Validate.SafePath(folder, "folder") is object folderErr) return folderErr;

        var guids = AssetDatabase.FindAssets(searchFilter, new[] { folder });
        var reimported = new List<string>();

        foreach (var guid in guids.Take(limit))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset != null) WorkflowManager.SnapshotObject(asset);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            reimported.Add(path);
        }

        var output = new Dictionary<string, object>
        {
            ["success"] = true,
            ["count"] = reimported.Count,
            ["assets"] = reimported
        };

        if (reimported.Any(ServerAvailabilityHelper.AffectsScriptDomain))
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                output,
                "Batch reimport included script-domain assets. Unity may briefly reload the script domain.",
                alwaysInclude: true);
        }
        else
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                output,
                "Batch reimport completed. Unity may still be refreshing assets.",
                alwaysInclude: false);
        }

        return output;
    }
}
```

## Notes

- `searchFilter` follows AssetDatabase syntax: `"t:Texture2D"`, `"t:AudioClip label:sfx"`, etc.
- `limit` prevents accidentally reimporting thousands of assets in one call.
- Returns the list of paths that were reimported.
