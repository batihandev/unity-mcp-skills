# model_get_settings

Read the full importer settings for a model asset.

**Skill ID:** `model_get_settings`
**Source:** `ModelSkills.cs` — `ModelGetSettings`

## Signature

```
model_get_settings(assetPath: string)
  → { success, path, globalScale, useFileScale, importBlendShapes, importVisibility,
      importCameras, importLights, meshCompression, isReadable, optimizeMeshPolygons,
      optimizeMeshVertices, generateSecondaryUV, keepQuads, weldVertices,
      importNormals, importTangents, animationType, importAnimation, materialImportMode }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the model file |

**Prerequisites:** [`validate`](../_shared/validate.md)

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) return err;

        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            return new { error = $"Not a model file or asset not found: {assetPath}" };

        return new
        {
            success = true,
            path = assetPath,
            // Scene
            globalScale = importer.globalScale,
            useFileScale = importer.useFileScale,
            importBlendShapes = importer.importBlendShapes,
            importVisibility = importer.importVisibility,
            importCameras = importer.importCameras,
            importLights = importer.importLights,
            // Meshes
            meshCompression = importer.meshCompression.ToString(),
            isReadable = importer.isReadable,
            optimizeMeshPolygons = importer.optimizeMeshPolygons,
            optimizeMeshVertices = importer.optimizeMeshVertices,
            generateSecondaryUV = importer.generateSecondaryUV,
            // Geometry
            keepQuads = importer.keepQuads,
            weldVertices = importer.weldVertices,
            // Normals & Tangents
            importNormals = importer.importNormals.ToString(),
            importTangents = importer.importTangents.ToString(),
            // Animation
            animationType = importer.animationType.ToString(),
            importAnimation = importer.importAnimation,
            // Materials
            materialImportMode = importer.materialImportMode.ToString()
        };
    }
}
```

## Notes

- Read-only; no reimport triggered.
- For a lighter read of just scale/compression/animationType, use `model_get_import_settings`.
