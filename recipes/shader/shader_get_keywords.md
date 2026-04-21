# shader_get_keywords

Get the list of keywords declared in a shader's keyword space.

**Signature:** `ShaderGetKeywords(string shaderNameOrPath)`

**Returns:** `{ shaderName, keywordCount, keywords: [{ name, type }] }`

## Notes

- Accepts either a shader's internal name or an asset path ending in `.shader`.
- `type` reflects Unity's `ShaderKeyword.type` (e.g., `BuiltinDefault`, `UserDefined`).
- To enable or disable a global keyword, use `shader_set_global_keyword`.
- To control per-material keywords, use `material_set_keyword` from the material module.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shaderNameOrPath = "Universal Render Pipeline/Lit"; // or "Assets/Shaders/My.shader"

        /* Original Logic:

            var shader = FindShaderByNameOrPath(shaderNameOrPath);
            if (shader == null) return new { error = $"Shader not found: {shaderNameOrPath}" };
            var keywords = shader.keywordSpace.keywords.Select(k => new { name = k.name, type = k.type.ToString() }).ToArray();
            return new { shaderName = shader.name, keywordCount = keywords.Length, keywords };
        */
    }
}
```
