# material_set_shader

Change the shader of a material.

**Signature:** `MaterialSetShader(string name = null, int instanceId = 0, string path = null, string shaderName = null)`

**Returns:** `{ success, target, shader }`

## Notes

- `shaderName` is required. Returns an error with a suggestion if the shader is not found.
- Use `project_get_render_pipeline` to identify the active pipeline and its recommended shaders.
- Switching shaders may reset property values; verify with `material_get_properties` afterwards.

## Common Shader Names

| Pipeline | Shader Name |
|----------|-------------|
| Standard (Built-in) | `Standard` |
| URP | `Universal Render Pipeline/Lit` |
| HDRP | `HDRP/Lit` |
| Unlit | `Unlit/Color`, `Unlit/Texture` |

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name       = "Cube";                          // target GameObject name
        int    instanceId = 0;
        string path       = null;                            // or material asset path
        string shaderName = "Universal Render Pipeline/Lit"; // required

        /* Original Logic:

            if (Validate.Required(shaderName, "shaderName") is object err) return err;

            var (material, go, error) = FindMaterial(name, instanceId, path);
            if (error != null) return error;

            var shader = Shader.Find(shaderName);
            if (shader == null)
                return new {
                    error = $"Shader not found: {shaderName}",
                    suggestion = "Use project_get_render_pipeline to see recommended shaders"
                };

            WorkflowManager.SnapshotObject(material);
            Undo.RecordObject(material, "Set Shader");
            material.shader = shader;

            if (go == null) EditorUtility.SetDirty(material);

            return new { success = true, target = go != null ? go.name : path, shader = shaderName };
        */
    }
}
```
