# shader_get_properties

Get all property definitions exposed by a shader (name, type, description).

**Signature:** `ShaderGetProperties(string shaderNameOrPath)`

**Returns:** `{ shaderName, propertyCount, properties: [{ name, type, description }] }`

## Notes

- Accepts either a shader's internal name (e.g., `"Standard"`) or an asset path ending in `.shader`.
- Returns property **definitions** from the shader source — not the current values set on a material instance.
- For material instance property values, use `material_get_properties` from the material module.
- `type` reflects `ShaderUtil.ShaderPropertyType` (e.g., `Color`, `Float`, `Range`, `TexEnv`, `Vector`).

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shaderNameOrPath = "Standard"; // or "Assets/Shaders/MyShader.shader"

        var shader = FindShaderByNameOrPath(shaderNameOrPath);
        if (shader == null) { result.SetResult(new { error = $"Shader not found: {shaderNameOrPath}" }); return; }

        var propCount = ShaderUtil.GetPropertyCount(shader);
        var properties = Enumerable.Range(0, propCount)
            .Select(i => new
            {
                name = ShaderUtil.GetPropertyName(shader, i),
                type = ShaderUtil.GetPropertyType(shader, i).ToString(),
                description = ShaderUtil.GetPropertyDescription(shader, i)
            })
            .ToArray();

        result.SetResult(new { shaderName = shader.name, propertyCount = propCount, properties });
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
