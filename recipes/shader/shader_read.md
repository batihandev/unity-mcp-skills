# shader_read

Read the full source code of a shader file from disk.

**Signature:** `ShaderRead(string shaderPath)`

**Returns:** `{ path, lines, content }`

## Notes

- `shaderPath` must be a valid `Assets/`-rooted path to a `.shader` file.
- Returns an error if the file does not exist.
- `lines` is the number of newline-separated lines in the file.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shaderPath = "Assets/Shaders/MyShader.shader";

        if (Validate.SafePath(shaderPath, "shaderPath") is object pathErr) { result.SetResult(pathErr); return; }
        if (!File.Exists(shaderPath))
            { result.SetResult(new { error = $"Shader not found: {shaderPath}" }); return; }

        var content = File.ReadAllText(shaderPath, System.Text.Encoding.UTF8);
        var lines = content.Split('\n').Length;

        { result.SetResult(new { path = shaderPath, lines, content }); return; }
    }
}
```
