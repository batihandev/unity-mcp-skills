# asset_import

Import an external file into the Unity project.

**Signature:** `AssetImport(string sourcePath, string destinationPath)`

**Returns:** `{ success, imported }` — plus a `serverAvailability` notice if the asset touches the script domain.

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string sourcePath = "/absolute/path/to/source/file.png"; // External file — must exist and be a file, not a directory
        string destinationPath = "Assets/Textures/file.png";     // Project-relative destination path

        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
        {
            result.SetResult(new { error = $"Source not found: {sourcePath}" });
            return;
        }
        if (Directory.Exists(sourcePath))
        {
            result.SetResult(new { error = $"Source path must be a file, not a directory: {sourcePath}" });
            return;
        }
        if (Validate.SafePath(destinationPath, "destinationPath") is object pathErr)
        {
            result.SetResult(pathErr);
            return;
        }

        var dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.Copy(sourcePath, destinationPath, true);
        AssetDatabase.ImportAsset(destinationPath);

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destinationPath);
        if (asset != null) WorkflowManager.SnapshotCreatedAsset(asset);

        var res = new Dictionary<string, object>
        {
            ["success"] = true,
            ["imported"] = destinationPath
        };

        if (ServerAvailabilityHelper.AffectsScriptDomain(destinationPath))
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                res,
                $"Imported script-domain asset: {destinationPath}. Unity may briefly reload the script domain.",
                alwaysInclude: true);
        }
        else
        {
            ServerAvailabilityHelper.AttachTransientUnavailableNotice(
                res,
                $"Asset import completed: {destinationPath}. Unity may still be refreshing assets.",
                alwaysInclude: false);
        }

        result.SetResult(res);
    }
}
```
