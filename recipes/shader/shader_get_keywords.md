# shader_get_keywords

Get the list of keywords declared in a shader's keyword space.

**Signature:** `ShaderGetKeywords(string shaderNameOrPath)`

**Returns:** `{ shaderName, keywordCount, keywords: [{ name, type }] }`

## Notes

- Accepts either a shader's internal name or an asset path ending in `.shader`.
- `type` reflects Unity's `ShaderKeyword.type` (e.g., `BuiltinDefault`, `UserDefined`).
- To enable or disable a global keyword, use `shader_set_global_keyword`.
- To control per-material keywords, use `material_set_keyword` from the material module.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shaderNameOrPath = "Universal Render Pipeline/Lit"; // or "Assets/Shaders/My.shader"

        var shader = FindShaderByNameOrPath(shaderNameOrPath);
        if (shader == null) { result.SetResult(new { error = $"Shader not found: {shaderNameOrPath}" }); return; }
        var keywords = shader.keywordSpace.keywords.Select(k => new { name = k.name, type = k.type.ToString() }).ToArray();
        result.SetResult(new { shaderName = shader.name, keywordCount = keywords.Length, keywords });
    }

    private static Shader FindShaderByNameOrPath(string shaderNameOrPath)
    {
        if (string.IsNullOrEmpty(shaderNameOrPath)) return null;
        Shader shader = null;
        if (shaderNameOrPath.EndsWith(".shader"))
            shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderNameOrPath);
        if (shader == null)
            shader = Shader.Find(shaderNameOrPath);
        return shader;
    }
}
```
