# asset_import

Import an external file into the Unity project.

**Signature:** `AssetImport(string sourcePath, string destinationPath)`

**Returns:** `{ success, imported }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string sourcePath = "/absolute/path/to/source/file.png";
        string destinationPath = "Assets/Textures/file.png";

        if (!File.Exists(sourcePath)) { result.SetResult(new { error = $"Source not found: {sourcePath}" }); return; }
        if (Validate.SafePath(destinationPath, "destinationPath") is object pathErr) { result.SetResult(pathErr); return; }

        var dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        File.Copy(sourcePath, destinationPath, true);
        AssetDatabase.ImportAsset(destinationPath);

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destinationPath);
        if (asset != null) WorkflowManager.SnapshotCreatedAsset(asset);

        result.SetResult(new { success = true, imported = destinationPath });
    }
}
```
