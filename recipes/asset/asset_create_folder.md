# asset_create_folder

Create a new folder in the project. Errors if the folder already exists.

**Signature:** `AssetCreateFolder(string folderPath)`

**Returns:** `{ success, path, guid }`

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string folderPath = "Assets/Art/Characters"; // Full project-relative folder path to create

        if (Validate.SafePath(folderPath, "folderPath") is object pathErr)
        {
            result.SetResult(pathErr);
            return;
        }
        if (Directory.Exists(folderPath))
        {
            result.SetResult(new { error = "Folder already exists" });
            return;
        }

        var parent = Path.GetDirectoryName(folderPath);
        var name = Path.GetFileName(folderPath);
        var guid = AssetDatabase.CreateFolder(parent, name);

        var createdAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
        if (createdAsset != null) WorkflowManager.SnapshotCreatedAsset(createdAsset);

        result.SetResult(new { success = true, path = folderPath, guid });
    }
}
```
