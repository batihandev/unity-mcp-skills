# texture_set_import_settings

Alternative bridge setter for common texture importer fields.

**Skill ID:** `texture_set_import_settings`
**Source:** `AssetImportSkills.cs` — `TextureSetImportSettings`

## Signature

```
texture_set_import_settings(
  assetPath: string,
  maxSize?: int,
  compression?: string,
  readable?: bool,
  generateMipMaps?: bool,
  textureType?: string
) → { success, assetPath, maxSize, compression, readable, mipmaps }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the texture |
| `maxSize` | int | no | Max texture size (e.g. 512, 1024, 2048) |
| `compression` | string | no | `None`, `LowQuality`, `NormalQuality`, `HighQuality` |
| `readable` | bool | no | CPU-readable flag |
| `generateMipMaps` | bool | no | Generate mipmaps |
| `textureType` | string | no | `Default`, `NormalMap`, `Sprite`, `Cursor`, `Cookie`, `Lightmap` |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/hero.png"; // Replace
        int? maxSize = null;
        string compression = null; // "None", "LowQuality", "NormalQuality", "HighQuality"
        bool? readable = null;
        bool? generateMipMaps = null;
        string textureType = null;

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return new { success = false, error = $"Not a texture or not found: {assetPath}" };

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        bool changed = false;

        if (maxSize.HasValue) { importer.maxTextureSize = maxSize.Value; changed = true; }

        if (!string.IsNullOrEmpty(compression))
        {
            switch (compression.ToLower())
            {
                case "none":          importer.textureCompression = TextureImporterCompression.Uncompressed; break;
                case "lowquality":    importer.textureCompression = TextureImporterCompression.CompressedLQ; break;
                case "normalquality": importer.textureCompression = TextureImporterCompression.Compressed;   break;
                case "highquality":   importer.textureCompression = TextureImporterCompression.CompressedHQ; break;
            }
            changed = true;
        }

        if (readable.HasValue)       { importer.isReadable    = readable.Value;       changed = true; }
        if (generateMipMaps.HasValue){ importer.mipmapEnabled = generateMipMaps.Value; changed = true; }

        if (!string.IsNullOrEmpty(textureType))
        {
            switch (textureType.ToLower())
            {
                case "default":   importer.textureType = TextureImporterType.Default;   break;
                case "normalmap": importer.textureType = TextureImporterType.NormalMap;  break;
                case "sprite":    importer.textureType = TextureImporterType.Sprite;     break;
                case "cursor":    importer.textureType = TextureImporterType.Cursor;     break;
                case "cookie":    importer.textureType = TextureImporterType.Cookie;     break;
                case "lightmap":  importer.textureType = TextureImporterType.Lightmap;   break;
            }
            changed = true;
        }

        if (changed) importer.SaveAndReimport();

        return new
        {
            success = true,
            assetPath,
            maxSize = importer.maxTextureSize,
            compression = importer.textureCompression.ToString(),
            readable = importer.isReadable,
            mipmaps = importer.mipmapEnabled
        };
    }
}
```

## Notes

- This bridge setter uses `importer.maxTextureSize` (global field) rather than the platform-settings object.
- For the full-featured setter with wrapMode, filterMode, sRGB, etc., use `texture_set_settings`.
- Note: `compression` values here differ in case from `TextureImporterCompression` enum — the switch handles case-insensitive matching.
