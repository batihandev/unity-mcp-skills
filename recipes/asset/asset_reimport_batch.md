# asset_reimport_batch

Reimport multiple assets matching a search filter, scoped to a folder, up to a limit.

**Signature:** `AssetReimportBatch(string searchFilter = "*", string folder = "Assets", int limit = 100)`

**Returns:** `{ success, count, assets: [path] }` — plus a `serverAvailability` notice if any reimported asset touches the script domain.

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string searchFilter = "t:Texture2D"; // AssetDatabase search filter (default "*" = all)
        string folder = "Assets/Textures";   // Root folder to search (default "Assets")
        int limit = 100;                     // Max assets to reimport (default 100)

        if (Validate.SafePath(folder, "folder") is object folderErr)
        {
            result.SetResult(folderErr);
            return;
        }

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

        var res = new Dictionary<string, object>
        {
            ["success"] = true,
            ["count"] = reimported.Count,
            ["assets"] = reimported
        };

        if (reimported.Any(ServerAvailabilityHelper.AffectsScriptDomain))
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                res,
                "Batch reimport included script-domain assets. Unity may briefly reload the script domain.",
                alwaysInclude: true);
        }
        else
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                res,
                "Batch reimport completed. Unity may still be refreshing assets.",
                alwaysInclude: false);
        }

        result.SetResult(res);
    }
}
```
