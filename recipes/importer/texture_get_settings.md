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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/hero.png"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            { result.SetResult(new { error = $"Not a texture or asset not found: {assetPath}" }); return; }

        var platformSettings = importer.GetDefaultPlatformTextureSettings();

        { result.SetResult(new
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
        }); return; }
    }
}
```

## Notes

- Returns the default platform settings for `maxTextureSize` and `compression`.
- For per-platform overrides, use `texture_get_platform_settings`.
- This is a read-only call; no reimport is triggered.
