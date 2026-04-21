# shader_check_errors

Check a shader for compilation errors using Unity's ShaderUtil message count.

**Signature:** `ShaderCheckErrors(string shaderNameOrPath)`

**Returns:** `{ shaderName, hasErrors, messageCount }`

## Notes

- Accepts either a shader's internal name or an asset path ending in `.shader`.
- `hasErrors` is `true` when `messageCount > 0`.
- `messageCount` includes warnings and errors reported by `ShaderUtil.GetShaderMessageCount`.
- Use this before assigning a shader to a material to confirm it compiled cleanly.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shaderNameOrPath = "Custom/MyShader"; // or "Assets/Shaders/My.shader"

        /* Original Logic:

            var shader = FindShaderByNameOrPath(shaderNameOrPath);
            if (shader == null) return new { error = $"Shader not found: {shaderNameOrPath}" };
            int msgCount = ShaderUtil.GetShaderMessageCount(shader);
            return new { shaderName = shader.name, hasErrors = msgCount > 0, messageCount = msgCount };
        */
    }
}
```
