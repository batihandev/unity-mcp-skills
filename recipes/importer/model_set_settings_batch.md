# model_set_settings_batch

Apply model importer settings to multiple model assets in one call.

**Skill ID:** `model_set_settings_batch`
**Source:** `ModelSkills.cs` — `ModelSetSettingsBatch`

## Signature

```
model_set_settings_batch(items: string) → { results[], errors[] }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `items` | string | yes | JSON array of item objects (see schema below) |

### Item Schema

Each object in the JSON array may contain:

| Field | Type | Description |
|-------|------|-------------|
| `assetPath` | string | Project-relative path (required per item) |
| `globalScale` | float | Import scale factor |
| `importBlendShapes` | bool | Import blend shapes |
| `importCameras` | bool | Import embedded cameras |
| `importLights` | bool | Import embedded lights |
| `isReadable` | bool | CPU-readable mesh |
| `generateSecondaryUV` | bool | Generate lightmap UVs |
| `importAnimation` | bool | Import animation |
| `meshCompression` | string | `Off`, `Low`, `Medium`, `High` |
| `animationType` | string | `None`, `Legacy`, `Generic`, `Humanoid` |
| `materialImportMode` | string | `None`, `ImportViaMaterialDescription`, `ImportStandard` |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.SnapshotObject`

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchModelItem
{
    public string assetPath;
    public float? globalScale;
    public bool? importBlendShapes;
    public bool? importCameras;
    public bool? importLights;
    public bool? isReadable;
    public bool? generateSecondaryUV;
    public bool? importAnimation;
    public string meshCompression;
    public string animationType;
    public string materialImportMode;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchModelItem { assetPath = "Assets/Models/hero.fbx", animationType = "Humanoid" },
            new _BatchModelItem { assetPath = "Assets/Models/table.fbx", importCameras = false, importLights = false, importAnimation = false },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var item in items)
            {
                var importer = AssetImporter.GetAtPath(item.assetPath) as ModelImporter;
                if (importer == null) { results.Add(new { path = item.assetPath, success = false, error = "Not a model file" }); failCount++; continue; }

                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.assetPath);
                if (asset != null) WorkflowManager.SnapshotObject(asset);

                if (item.globalScale.HasValue) importer.globalScale = item.globalScale.Value;
                if (item.importBlendShapes.HasValue) importer.importBlendShapes = item.importBlendShapes.Value;
                if (item.importCameras.HasValue) importer.importCameras = item.importCameras.Value;
                if (item.importLights.HasValue) importer.importLights = item.importLights.Value;
                if (item.isReadable.HasValue) importer.isReadable = item.isReadable.Value;
                if (item.generateSecondaryUV.HasValue) importer.generateSecondaryUV = item.generateSecondaryUV.Value;
                if (item.importAnimation.HasValue) importer.importAnimation = item.importAnimation.Value;

                if (!string.IsNullOrEmpty(item.meshCompression) &&
                    System.Enum.TryParse<ModelImporterMeshCompression>(item.meshCompression, true, out var mc))
                    importer.meshCompression = mc;
                if (!string.IsNullOrEmpty(item.animationType) &&
                    System.Enum.TryParse<ModelImporterAnimationType>(item.animationType, true, out var at))
                    importer.animationType = at;
                if (!string.IsNullOrEmpty(item.materialImportMode) &&
                    System.Enum.TryParse<ModelImporterMaterialImportMode>(item.materialImportMode, true, out var mim))
                    importer.materialImportMode = mim;

                importer.SaveAndReimport();
                results.Add(new { path = item.assetPath, success = true });
                successCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```

## Notes

- Wraps operations in `StartAssetEditing`/`StopAssetEditing` for faster batch processing.
- Errors on individual items are captured per-item; processing continues for remaining items.
