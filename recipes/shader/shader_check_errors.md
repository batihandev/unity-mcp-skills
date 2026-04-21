# shader_check_errors

Check a shader for compilation errors using Unity's ShaderUtil message count.

**Signature:** `ShaderCheckErrors(string shaderNameOrPath)`

**Returns:** `{ shaderName, hasErrors, messageCount }`

## Notes

- Accepts either a shader's internal name or an asset path ending in `.shader`.
- `hasErrors` is `true` when `messageCount > 0`.
- `messageCount` includes warnings and errors reported by `ShaderUtil.GetShaderMessageCount`.
- Use this before assigning a shader to a material to confirm it compiled cleanly.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shaderNameOrPath = "Custom/MyShader"; // or "Assets/Shaders/My.shader"

        var shader = FindShaderByNameOrPath(shaderNameOrPath);
        if (shader == null) { result.SetResult(new { error = $"Shader not found: {shaderNameOrPath}" }); return; }
        int msgCount = ShaderUtil.GetShaderMessageCount(shader);
        { result.SetResult(new { shaderName = shader.name, hasErrors = msgCount > 0, messageCount = msgCount }); return; }
    }
}
```
