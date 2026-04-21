# model_get_materials_info

Inspect sub-asset materials and meshes embedded in a model file.

**Skill ID:** `model_get_materials_info`
**Source:** `ModelSkills.cs` — `ModelGetMaterialsInfo`

## Signature

```
model_get_materials_info(assetPath: string)
  → { success, path, materialCount, materials[{ name, shader }],
      meshCount, meshes[{ name, vertices, triangles }] }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the model file |

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) return err;
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) return new { error = $"Not a model: {assetPath}" };

        var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

        var materials = allAssets.OfType<Material>()
            .Select(m => new { name = m.name, shader = m.shader != null ? m.shader.name : "null" })
            .ToArray();

        var meshes = allAssets.OfType<Mesh>()
            .Select(m => new { name = m.name, vertices = m.vertexCount, triangles = SkillsCommon.GetTriangleCount(m) })
            .ToArray();

        return new
        {
            success = true,
            path = assetPath,
            materialCount = materials.Length,
            materials,
            meshCount = meshes.Length,
            meshes
        };
    }
}
```

## Notes

- Returns sub-assets embedded in the model file itself; externally extracted materials are not listed here.
- Use `model_get_animations_info` to inspect embedded animation clips.
