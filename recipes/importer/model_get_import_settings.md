# model_get_import_settings

Read a minimal set of model importer settings (bridge getter).

**Skill ID:** `model_get_import_settings`
**Source:** `AssetImportSkills.cs` — `ModelGetImportSettings`

## Signature

```
model_get_import_settings(assetPath: string)
  → { success, assetPath, globalScale, importAnimation, importMaterials,
      meshCompression, readable, generateColliders }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the model |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx"; // Replace with target path

        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) { result.SetResult(new { error = $"Not a model: {assetPath}" }); return; }

        { result.SetResult(new
        {
            success = true,
            assetPath,
            globalScale = importer.globalScale,
            importAnimation = importer.importAnimation,
            importMaterials = importer.materialImportMode != ModelImporterMaterialImportMode.None,
            meshCompression = importer.meshCompression.ToString(),
            readable = importer.isReadable,
            generateColliders = importer.addCollider
        }); return; }
    }
}
```

## Notes

- `importMaterials` is a simplified bool (`true` when `materialImportMode != None`).
- For the full importer settings including normals, tangents, blend shapes, etc., use `model_get_settings`.
- Read-only; no reimport triggered.
