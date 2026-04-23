# model_get_settings

Read the full importer settings for a model asset.

## Signature

```
model_get_settings(assetPath: string)
  → { success, path, globalScale, useFileScale, importBlendShapes, importVisibility,
      importCameras, importLights, meshCompression, isReadable, optimizeMeshPolygons,
      optimizeMeshVertices, generateSecondaryUV, keepQuads, weldVertices,
      importNormals, importTangents, animationType, importAnimation, materialImportMode }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }

        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            { result.SetResult(new { error = $"Not a model file or asset not found: {assetPath}" }); return; }

        { result.SetResult(new
        {
            success = true,
            path = assetPath,
            globalScale = importer.globalScale,
            useFileScale = importer.useFileScale,
            importBlendShapes = importer.importBlendShapes,
            importVisibility = importer.importVisibility,
            importCameras = importer.importCameras,
            importLights = importer.importLights,
            meshCompression = importer.meshCompression.ToString(),
            isReadable = importer.isReadable,
            optimizeMeshPolygons = importer.optimizeMeshPolygons,
            optimizeMeshVertices = importer.optimizeMeshVertices,
            generateSecondaryUV = importer.generateSecondaryUV,
            keepQuads = importer.keepQuads,
            weldVertices = importer.weldVertices,
            importNormals = importer.importNormals.ToString(),
            importTangents = importer.importTangents.ToString(),
            animationType = importer.animationType.ToString(),
            importAnimation = importer.importAnimation,
            materialImportMode = importer.materialImportMode.ToString()
        }); return; }
    }
}
```

## Notes
- For a lighter read of just scale/compression/animationType, use `model_get_import_settings`.

