# asset_reimport

Force reimport of a single asset using `ImportAssetOptions.ForceUpdate`.

**Signature:** `AssetReimport(string assetPath)`

**Returns:** `{ success, reimported }`

**Note:** Accepts project-relative paths (e.g. `Assets/foo.cs`) and absolute paths — falls back to resolving under the project root.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Scripts/MyScript.cs";

        if (string.IsNullOrEmpty(assetPath)) { result.SetResult(new { success = false, error = "assetPath is required" }); return; }
        if (Validate.SafePath(assetPath, "assetPath") is object pathErr) { result.SetResult(pathErr); return; }

        if (!File.Exists(assetPath) && !Directory.Exists(assetPath))
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var fullPath = Path.Combine(projectRoot, assetPath);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            { result.SetResult(new { success = false, error = $"Asset not found: {assetPath}" }); return; }
        }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        result.SetResult(new { success = true, reimported = assetPath });
    }
}
```
