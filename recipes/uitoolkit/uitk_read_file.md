# uitk_read_file

Read the content of a USS or UXML file.

**Signature:** `UitkReadFile(filePath string)`

**Returns:** `{ path, type, lines, content }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filePath = "Assets/UI/MyStyle.uss";

        if (Validate.SafePath(filePath, "filePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (!File.Exists(filePath)) { result.SetResult(new { error = $"File not found: {filePath}" }); return; }

        var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        result.SetResult(new
        {
            path = filePath,
            type = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant(),
            lines = content.Split('\n').Length,
            content
        });
    }
}
```
