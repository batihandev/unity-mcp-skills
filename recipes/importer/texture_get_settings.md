# texture_get_settings

Read the full importer settings for a texture asset.

**Skill ID:** `texture_get_settings`
**Source:** `TextureSkills.cs` — `TextureGetSettings`

## Signature

```
texture_get_settings(assetPath: string)
  → { success, path, textureType, textureShape, sRGB, alphaSource, alphaIsTransparency,
      readable, mipmapEnabled, filterMode, wrapMode, maxTextureSize, compression,
      spriteMode, spritePixelsPerUnit, npotScale }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the texture asset |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/hero.png"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) return err;

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return new { error = $"Not a texture or asset not found: {assetPath}" };

        var platformSettings = importer.GetDefaultPlatformTextureSettings();

        return new
        {
            success = true,
            path = assetPath,
            textureType = importer.textureType.ToString(),
            textureShape = importer.textureShape.ToString(),
            sRGB = importer.sRGBTexture,
            alphaSource = importer.alphaSource.ToString(),
            alphaIsTransparency = importer.alphaIsTransparency,
            readable = importer.isReadable,
            mipmapEnabled = importer.mipmapEnabled,
            filterMode = importer.filterMode.ToString(),
            wrapMode = importer.wrapMode.ToString(),
            maxTextureSize = platformSettings.maxTextureSize,
            compression = platformSettings.textureCompression.ToString(),
            spriteMode = importer.spriteImportMode.ToString(),
            spritePixelsPerUnit = importer.spritePixelsPerUnit,
            npotScale = importer.npotScale.ToString()
        };
    }
}
```

## Notes

- Returns the default platform settings for `maxTextureSize` and `compression`.
- For per-platform overrides, use `texture_get_platform_settings`.
- This is a read-only call; no reimport is triggered.
