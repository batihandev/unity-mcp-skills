# shader_read

Read the full source code of a shader file from disk.

**Signature:** `ShaderRead(string shaderPath)`

**Returns:** `{ path, lines, content }`

## Notes

- `shaderPath` must be a valid `Assets/`-rooted path to a `.shader` file.
- Returns an error if the file does not exist.
- `lines` is the number of newline-separated lines in the file.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shaderPath = "Assets/Shaders/MyShader.shader";

        /* Original Logic:

            if (Validate.SafePath(shaderPath, "shaderPath") is object pathErr) return pathErr;
            if (!File.Exists(shaderPath))
                return new { error = $"Shader not found: {shaderPath}" };

            var content = File.ReadAllText(shaderPath, System.Text.Encoding.UTF8);
            var lines = content.Split('\n').Length;

            return new { path = shaderPath, lines, content };
        */
    }
}
```
