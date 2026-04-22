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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

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

        if (Validate.Required(shaderName, "shaderName") is object err) { result.SetResult(err); return; }

        var (material, go, error) = FindMaterial(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var shader = Shader.Find(shaderName);
        if (shader == null)
        {
            { result.SetResult(new {
                error = $"Shader not found: {shaderName}",
                suggestion = "Use project_get_render_pipeline to see recommended shaders"
            }); return; }
        }

        WorkflowManager.SnapshotObject(material);
        Undo.RecordObject(material, "Set Shader");
        material.shader = shader;

        if (go == null) EditorUtility.SetDirty(material);

        { result.SetResult(new { 
            success = true, 
            target = go != null ? go.name : path, 
            shader = shaderName
        }); return; }
    }

    private static (Material mat, GameObject go, object error) FindMaterial(string name, int instanceId, string path)
    {
        if (!string.IsNullOrEmpty(path) && path.EndsWith(".mat"))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) return (null, null, new { error = "Material asset not found: " + path });
            return (m, null, null);
        }
        var (go, err) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (err != null) return (null, null, err);
        var rdr = go.GetComponent<Renderer>();
        if (rdr == null) return (null, go, new { error = "No Renderer on " + go.name });
        var mat = rdr.sharedMaterial;
        if (mat == null) return (null, go, new { error = "No material on " + go.name });
        return (mat, go, null);
    }
}
```
