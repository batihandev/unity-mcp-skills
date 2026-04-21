# uitk_write_file

Overwrite the full content of a USS or UXML file (creates it if it does not exist).

**Signature:** `UitkWriteFile(filePath string, content string)`

**Returns:** `{ success, path, lines }`

**Notes:**
- Unlike `uitk_create_uss`/`uitk_create_uxml`, this command does not fail if the file already exists — it overwrites it.
- Creates intermediate directories automatically.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filePath = "Assets/UI/MyStyle.uss";
        string content = ".my-class { color: white; }";

        if (Validate.SafePath(filePath, "filePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (Validate.Required(content, "content") is object contentErr) { result.SetResult(contentErr); return; }

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(filePath))
        {
            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
            if (existing != null) WorkflowManager.SnapshotObject(existing);
        }

        File.WriteAllText(filePath, content, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(filePath);

        result.SetResult(new { success = true, path = filePath, lines = content.Split('\n').Length });
    }
}
```
