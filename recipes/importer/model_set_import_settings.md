# model_set_import_settings

Alternative bridge setter for common model importer fields.

**Skill ID:** `model_set_import_settings`
**Source:** `AssetImportSkills.cs` — `ModelSetImportSettings`

## Signature

```
model_set_import_settings(
  assetPath: string,
  globalScale?: float,
  importMaterials?: bool,
  importAnimation?: bool,
  generateColliders?: bool,
  readable?: bool,
  meshCompression?: string
) → { success, assetPath, globalScale, importAnimation, meshCompression }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the model |
| `globalScale` | float | no | Import scale factor |
| `importMaterials` | bool | no | `true` = `ImportViaMaterialDescription`, `false` = `None` |
| `importAnimation` | bool | no | Import animation clips |
| `generateColliders` | bool | no | Add mesh colliders (`addCollider`) |
| `readable` | bool | no | CPU-readable mesh data |
| `meshCompression` | string | no | `Off`, `Low`, `Medium`, `High` |

**Prerequisites:** [`workflow_manager`](../_shared/workflow_manager.md)

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx"; // Replace
        float? globalScale = null;
        bool? importMaterials = null;
        bool? importAnimation = null;
        bool? generateColliders = null;
        bool? readable = null;
        string meshCompression = null; // "Off" | "Low" | "Medium" | "High"

        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            return new { success = false, error = $"Not a model or not found: {assetPath}" };

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        bool changed = false;

        if (globalScale.HasValue)        { importer.globalScale    = globalScale.Value;    changed = true; }
        if (importAnimation.HasValue)    { importer.importAnimation = importAnimation.Value; changed = true; }
        if (generateColliders.HasValue)  { importer.addCollider    = generateColliders.Value; changed = true; }
        if (readable.HasValue)           { importer.isReadable     = readable.Value;       changed = true; }

        if (importMaterials.HasValue)
        {
            importer.materialImportMode = importMaterials.Value
                ? ModelImporterMaterialImportMode.ImportViaMaterialDescription
                : ModelImporterMaterialImportMode.None;
            changed = true;
        }

        if (!string.IsNullOrEmpty(meshCompression))
        {
            switch (meshCompression.ToLower())
            {
                case "off":    importer.meshCompression = ModelImporterMeshCompression.Off;    break;
                case "low":    importer.meshCompression = ModelImporterMeshCompression.Low;    break;
                case "medium": importer.meshCompression = ModelImporterMeshCompression.Medium; break;
                case "high":   importer.meshCompression = ModelImporterMeshCompression.High;   break;
            }
            changed = true;
        }

        if (changed) importer.SaveAndReimport();

        return new
        {
            success = true,
            assetPath,
            globalScale = importer.globalScale,
            importAnimation = importer.importAnimation,
            meshCompression = importer.meshCompression.ToString()
        };
    }
}
```

## Notes

- `importMaterials` is a simplified bool shorthand. For full `materialImportMode` string control, use `model_set_settings`.
- `generateColliders` maps to `importer.addCollider`.
- For the full setter with normals, tangents, blend shapes, etc., use `model_set_settings`.
