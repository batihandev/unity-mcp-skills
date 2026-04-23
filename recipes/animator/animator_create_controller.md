# animator_create_controller

Create a new Animator Controller asset.

**Signature:** `AnimatorCreateController(string name, string folder = "Assets/Animations")`

**Returns:** `{ success, name, path }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyController"; // Controller name — must not contain path separators
        string folder = "Assets/Animations"; // Destination folder

        if (Validate.Required(name, "name") is object nameErr) { result.SetResult(nameErr); return; }
        if (name.Contains("/") || name.Contains("\\") || name.Contains(".."))
        {
            result.SetResult(new { error = "name must not contain path separators" });
            return;
        }

        var folderErr = Validate.SafePath(folder, "folder");
        if (folderErr != null) { result.SetResult(folderErr); return; }

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var path = Path.Combine(folder, name + ".controller");
        if (File.Exists(path))
        {
            result.SetResult(new { error = $"Controller already exists: {path}" });
            return;
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        WorkflowManager.SnapshotObject(controller, SnapshotType.Created);
        AssetDatabase.SaveAssets();

        result.SetResult(new { success = true, name, path });
    }
}
```
