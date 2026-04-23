# texture_set_settings_batch

Apply texture importer settings to multiple texture assets in one call.

## Signature

```
texture_set_settings_batch(items: string) → { results[], errors[] }
```

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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchTextureItem
{
    public string assetPath;
    public string textureType;
    public string filterMode;
    public string compression;
    public int? maxSize;
    public bool? mipmapEnabled;
    public bool? sRGB;
    public bool? readable;
    public float? spritePixelsPerUnit;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchTextureItem { assetPath = "Assets/Textures/hero.png", textureType = "Sprite", maxSize = 1024 },
            new _BatchTextureItem { assetPath = "Assets/Textures/bg.png", filterMode = "Point", mipmapEnabled = false },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var item in items)
            {
                var importer = AssetImporter.GetAtPath(item.assetPath) as TextureImporter;
                if (importer == null) { results.Add(new { path = item.assetPath, success = false, error = "Not a texture" }); failCount++; continue; }

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

- Uses `AssetDatabase.StartAssetEditing()` / `StopAssetEditing()` to batch the import operations, which is significantly faster for large sets.
- Errors on individual items are captured per-item; processing continues for remaining items.
