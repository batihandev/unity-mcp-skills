# material_get_properties

Get all properties of a material (colors, floats, vectors, textures, integers, and keywords).

**Signature:** `MaterialGetProperties(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, target, shader, renderQueue, keywords, giFlags, properties: { colors, floats, vectors, textures, integers } }`

## Notes

- Read-only: does not modify the material.
- Each property entry includes `name`, `description`, and `value`. Float and Range properties also include `min` and `max`.
- `keywords` is the raw array of enabled shader keywords.
- Use this to discover property names before calling setters like `material_set_float` or `material_set_color`.

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
        string name       = "Cube"; // target GameObject name
        int    instanceId = 0;
        string path       = null;   // or material asset path like "Assets/Materials/M.mat"

        var (material, go, error) = FindMaterial(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var shader = material.shader;
        int propertyCount = shader.GetPropertyCount();

        var colors = new List<object>();
        var floats = new List<object>();
        var vectors = new List<object>();
        var textures = new List<object>();
        var integers = new List<object>();

        for (int i = 0; i < propertyCount; i++)
        {
            var propName = shader.GetPropertyName(i);
            var propType = shader.GetPropertyType(i);
            var propDesc = shader.GetPropertyDescription(i);
    
            switch (propType)
            {
                case UnityEngine.Rendering.ShaderPropertyType.Color:
                    var color = material.GetColor(propName);
                    colors.Add(new { name = propName, description = propDesc, value = new { r = color.r, g = color.g, b = color.b, a = color.a } });
                    break;
                case UnityEngine.Rendering.ShaderPropertyType.Float:
                    floats.Add(new { name = propName, description = propDesc, value = material.GetFloat(propName), min = 0f, max = 0f });
                    break;
                case UnityEngine.Rendering.ShaderPropertyType.Range:
                    var range = shader.GetPropertyRangeLimits(i);
                    floats.Add(new { name = propName, description = propDesc, value = material.GetFloat(propName), min = range.x, max = range.y });
                    break;
                case UnityEngine.Rendering.ShaderPropertyType.Vector:
                    var vec = material.GetVector(propName);
                    vectors.Add(new { name = propName, description = propDesc, value = new { x = vec.x, y = vec.y, z = vec.z, w = vec.w } });
                    break;
                case UnityEngine.Rendering.ShaderPropertyType.Texture:
                    var tex = material.GetTexture(propName);
                    textures.Add(new { name = propName, description = propDesc, value = tex != null ? tex.name : null });
                    break;
                case UnityEngine.Rendering.ShaderPropertyType.Int:
                    integers.Add(new { name = propName, description = propDesc, value = material.GetInt(propName) });
                    break;
            }
        }

        { result.SetResult(new {
            success = true,
            target = go != null ? go.name : path,
            shader = shader.name,
            renderQueue = material.renderQueue,
            keywords = material.shaderKeywords,
            giFlags = material.globalIlluminationFlags.ToString(),
            properties = new {
                colors,
                floats,
                vectors,
                textures,
                integers
            }
        }); return; }
    }
}
```
