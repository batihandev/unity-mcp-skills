# texture_set_settings_batch

Apply texture importer settings to multiple texture assets in one call.

**Skill ID:** `texture_set_settings_batch`
**Source:** `TextureSkills.cs` — `TextureSetSettingsBatch`

## Signature

```
texture_set_settings_batch(items: string) → { results[], errors[] }
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
| `textureType` | string | `Default`, `NormalMap`, `Sprite`, etc. |
| `filterMode` | string | `Point`, `Bilinear`, `Trilinear` |
| `compression` | string | `None`, `LowQuality`, `NormalQuality`, `HighQuality` |
| `maxSize` | int | Max texture dimension |
| `mipmapEnabled` | bool | Generate mipmaps |
| `sRGB` | bool | sRGB colour space |
| `readable` | bool | CPU-readable |
| `spritePixelsPerUnit` | float | Pixels per unit |

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // JSON array of BatchTextureItem objects
        string items = @"[
            { ""assetPath"": ""Assets/Textures/hero.png"", ""textureType"": ""Sprite"", ""maxSize"": 1024 },
            { ""assetPath"": ""Assets/Textures/bg.png"",   ""filterMode"": ""Point"", ""mipmapEnabled"": false }
        ]";

        return BatchExecutor.Execute<BatchTextureItem>(items, item =>
        {
            var importer = AssetImporter.GetAtPath(item.assetPath) as TextureImporter;
            if (importer == null)
                throw new System.Exception("Not a texture");

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.assetPath);
            if (asset != null) WorkflowManager.SnapshotObject(asset);

            if (!string.IsNullOrEmpty(item.textureType) &&
                System.Enum.TryParse<TextureImporterType>(item.textureType.Replace(" ", ""), true, out var tt))
                importer.textureType = tt;

            if (!string.IsNullOrEmpty(item.filterMode) &&
                System.Enum.TryParse<FilterMode>(item.filterMode, true, out var fm))
                importer.filterMode = fm;

            if (item.mipmapEnabled.HasValue) importer.mipmapEnabled = item.mipmapEnabled.Value;
            if (item.sRGB.HasValue) importer.sRGBTexture = item.sRGB.Value;
            if (item.readable.HasValue) importer.isReadable = item.readable.Value;
            if (item.spritePixelsPerUnit.HasValue) importer.spritePixelsPerUnit = item.spritePixelsPerUnit.Value;

            if (item.maxSize.HasValue || !string.IsNullOrEmpty(item.compression))
            {
                var ps = importer.GetDefaultPlatformTextureSettings();
                if (item.maxSize.HasValue) ps.maxTextureSize = item.maxSize.Value;
                if (!string.IsNullOrEmpty(item.compression) &&
                    System.Enum.TryParse<TextureImporterCompression>(item.compression, true, out var tc))
                    ps.textureCompression = tc;
                importer.SetPlatformTextureSettings(ps);
            }

            importer.SaveAndReimport();
            return new { path = item.assetPath, success = true };
        }, item => item.assetPath,
        setup: () => AssetDatabase.StartAssetEditing(),
        teardown: () => { AssetDatabase.StopAssetEditing(); AssetDatabase.Refresh(); });
    }
}
```

## Notes

- Uses `AssetDatabase.StartAssetEditing()` / `StopAssetEditing()` to batch the import operations, which is significantly faster for large sets.
- Errors on individual items are captured per-item; processing continues for remaining items.
