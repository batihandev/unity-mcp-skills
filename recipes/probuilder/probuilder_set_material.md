# probuilder_set_material

Assign a material to an entire ProBuilder mesh, or apply a quick prototype color.

**Signature:** `ProBuilderSetMaterial(string name = null, int instanceId = 0, string path = null, string materialPath = null, float? r = null, float? g = null, float? b = null, float? a = null)`

**Returns:** `{ success, name, instanceId, material }` or `{ success, name, instanceId, materialName, color, note }` for runtime color.

## Notes

- Provide `materialPath` for a persistent material asset (e.g. `"Assets/Materials/MyMat.mat"`).
- Provide `r/g/b` (and optionally `a`) for a quick runtime color. The color is NOT saved as an asset.
- You must provide one or the other — both empty is an error.
- For per-face material assignment use `probuilder_set_face_material`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        #if !PROBUILDER
                    { result.SetResult(NoProBuilder()); return; }
        #else
                    var (pbMesh, err) = FindProBuilderMesh(name, instanceId, path);
                    if (err != null) { result.SetResult(err); return; }

                    var renderer = pbMesh.GetComponent<MeshRenderer>();
                    if (renderer == null)
                        { result.SetResult(new { error = $"'{pbMesh.gameObject.name}' has no MeshRenderer component" }); return; }

                    Undo.RecordObject(renderer, "Set Material");
                    WorkflowManager.SnapshotObject(pbMesh.gameObject);

                    if (!string.IsNullOrEmpty(materialPath))
                    {
                        if (Validate.SafePath(materialPath, "materialPath") is object pathErr) { result.SetResult(pathErr); return; }
                        var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (mat == null)
                            { result.SetResult(new { error = $"Material not found: {materialPath}" }); return; }
                        renderer.sharedMaterial = mat;
                    }
                    else if (r.HasValue || g.HasValue || b.HasValue)
                    {
                        // Create a temporary colored material using current render pipeline's shader
                        var color = new Color(r ?? 0.5f, g ?? 0.5f, b ?? 0.5f, a ?? 1f);
                        var shaderName = ProjectSkills.GetDefaultShaderName();
                        var shader = Shader.Find(shaderName);
                        if (shader == null)
                            { result.SetResult(new { error = $"Cannot find shader '{shaderName}' for current render pipeline" }); return; }
                        var mat = new Material(shader);
                        var colorProp = ProjectSkills.GetColorPropertyName();
                        if (mat.HasProperty(colorProp))
                            mat.SetColor(colorProp, color);
                        else
                            mat.color = color;
                        mat.name = $"PB_{pbMesh.gameObject.name}_{ColorUtility.ToHtmlStringRGB(color)}";
                        renderer.sharedMaterial = mat;

                        { result.SetResult(new
                        {
                            success = true,
                            name = pbMesh.gameObject.name,
                            instanceId = pbMesh.gameObject.GetInstanceID(),
                            materialName = mat.name,
                            color = new { r = color.r, g = color.g, b = color.b, a = color.a },
                            note = "Runtime material created. Use material_create + materialPath for persistent materials."
                        }); return; }
                    }
                    else
                    {
                        { result.SetResult(new { error = "Provide materialPath or color (r,g,b)" }); return; }
                    }

                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        material = renderer.sharedMaterial.name
                    }); return; }
        #endif
    }
}
```
