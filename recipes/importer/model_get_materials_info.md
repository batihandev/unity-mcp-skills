# model_get_materials_info

Inspect sub-asset materials and meshes embedded in a model file.

## Signature

```
model_get_materials_info(assetPath: string)
  → { success, path, materialCount, materials[{ name, shader }],
      meshCount, meshes[{ name, vertices, triangles }] }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`skills_common`](../_shared/skills_common.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) { result.SetResult(new { error = $"Not a model: {assetPath}" }); return; }

        var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

        var materials = allAssets.OfType<Material>()
            .Select(m => new { name = m.name, shader = m.shader != null ? m.shader.name : "null" })
            .ToArray();

        var meshes = allAssets.OfType<UnityEngine.Mesh>()
            .Select(m => new { name = m.name, vertices = m.vertexCount, triangles = SkillsCommon.GetTriangleCount(m) })
            .ToArray();

        { result.SetResult(new
        {
            success = true,
            path = assetPath,
            materialCount = materials.Length,
            materials,
            meshCount = meshes.Length,
            meshes
        }); return; }
    }
}
```

## Notes

- Returns sub-assets embedded in the model file itself; externally extracted materials are not listed here.
- Use `model_get_animations_info` to inspect embedded animation clips.
