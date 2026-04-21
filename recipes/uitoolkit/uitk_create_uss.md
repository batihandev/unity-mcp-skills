# uitk_create_uss

Create a new USS stylesheet file for UI Toolkit.

**Signature:** `UitkCreateUss(savePath string, content string = null)`

**Returns:** `{ success, path, lines }`

**Notes:**
- Fails if the file already exists.
- Creates intermediate directories automatically.
- `content = null` produces a generated starter stylesheet with `:root` design tokens.

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string savePath = "Assets/UI/MyStyle.uss";
        string content = null; // null → skill writes a default USS starter file

        if (Validate.SafePath(savePath, "savePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (File.Exists(savePath)) { result.SetResult(new { error = $"File already exists: {savePath}" }); return; }

        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var fileName = Path.GetFileNameWithoutExtension(savePath);
        var fileContent = content ?? $"/* {fileName} Stylesheet */\n:root {{\n    --primary-color: #2D2D2D;\n    --text-color: #E0E0E0;\n    --accent-color: #4A90D9;\n}}\n";
        File.WriteAllText(savePath, fileContent, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset(savePath);

        result.SetResult(new { success = true, path = savePath, lines = fileContent.Split('\n').Length });
    }
}
```
