# shader_get_variant_count

Get the subshader and pass count for a shader as a proxy for variant complexity.

**Signature:** `ShaderGetVariantCount(string shaderNameOrPath)`

**Returns:** `{ shaderName, subshaderCount, totalPasses }`

## Notes

- Accepts either a shader's internal name or an asset path ending in `.shader`.
- `totalPasses` is the sum of pass counts across all subshaders — use this as a rough variant-complexity indicator.
- High pass counts can increase build times and GPU overhead; review shaders with many passes before shipping.
- This does not enumerate keyword-driven variants; it counts declared passes only.

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
        var data = ShaderUtil.GetShaderData(shader);
        int totalVariants = 0;
        int subshaderCount = data.SubshaderCount;
        for (int s = 0; s < subshaderCount; s++)
        {
            var sub = data.GetSubshader(s);
            totalVariants += sub.PassCount;
        }
        { result.SetResult(new { shaderName = shader.name, subshaderCount, totalPasses = totalVariants }); return; }
    }
}
```
