# animator_create_controller

Create a new Animator Controller asset.

**Signature:** `AnimatorCreateController(string name, string folder = "Assets/Animations")`

**Returns:** `{ success, name, path }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

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
