# asset_reimport_batch

Reimport multiple assets matching a search filter, scoped to a folder, up to a limit.

**Signature:** `AssetReimportBatch(string searchFilter = "*", string folder = "Assets", int limit = 100)`

**Returns:** `{ success, count, assets }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string searchFilter = "t:Texture2D";
        string folder = "Assets/Textures";
        int limit = 100;

        if (Validate.SafePath(folder, "folder") is object folderErr) { result.SetResult(folderErr); return; }

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

        result.SetResult(new { success = true, count = reimported.Count, assets = reimported });
    }
}
```
