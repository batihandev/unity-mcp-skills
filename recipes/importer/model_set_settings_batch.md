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

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // JSON array of BatchModelItem objects
        string items = @"[
            { ""assetPath"": ""Assets/Models/hero.fbx"",  ""animationType"": ""Humanoid"" },
            { ""assetPath"": ""Assets/Models/table.fbx"", ""importCameras"": false, ""importLights"": false, ""importAnimation"": false }
        ]";

        return BatchExecutor.Execute<BatchModelItem>(items, item =>
        {
            var importer = AssetImporter.GetAtPath(item.assetPath) as ModelImporter;
            if (importer == null)
                throw new System.Exception("Not a model file");

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.assetPath);
            if (asset != null) WorkflowManager.SnapshotObject(asset);

            if (item.globalScale.HasValue)       importer.globalScale       = item.globalScale.Value;
            if (item.importBlendShapes.HasValue)  importer.importBlendShapes  = item.importBlendShapes.Value;
            if (item.importCameras.HasValue)      importer.importCameras      = item.importCameras.Value;
            if (item.importLights.HasValue)       importer.importLights       = item.importLights.Value;
            if (item.isReadable.HasValue)         importer.isReadable         = item.isReadable.Value;
            if (item.generateSecondaryUV.HasValue) importer.generateSecondaryUV = item.generateSecondaryUV.Value;
            if (item.importAnimation.HasValue)    importer.importAnimation    = item.importAnimation.Value;

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
            return new { path = item.assetPath, success = true };
        }, item => item.assetPath,
        setup: () => AssetDatabase.StartAssetEditing(),
        teardown: () => { AssetDatabase.StopAssetEditing(); AssetDatabase.Refresh(); });
    }
}
```

## Notes

- Wraps operations in `StartAssetEditing`/`StopAssetEditing` for faster batch processing.
- Errors on individual items are captured per-item; processing continues for remaining items.
