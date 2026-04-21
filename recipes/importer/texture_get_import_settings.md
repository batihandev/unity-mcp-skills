# texture_get_import_settings

Read a minimal set of texture importer settings (bridge getter).

**Skill ID:** `texture_get_import_settings`
**Source:** `AssetImportSkills.cs` — `TextureGetImportSettings`

## Signature

```
texture_get_import_settings(assetPath: string)
  → { success, assetPath, textureType, maxSize, compression, readable, mipmaps, spriteMode, pixelsPerUnit }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the texture |

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/hero.png"; // Replace with target path

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return new { error = $"Not a texture: {assetPath}" };

        return new
        {
            success = true,
            assetPath,
            textureType = importer.textureType.ToString(),
            maxSize = importer.maxTextureSize,
            compression = importer.textureCompression.ToString(),
            readable = importer.isReadable,
            mipmaps = importer.mipmapEnabled,
            spriteMode = importer.spriteImportMode.ToString(),
            pixelsPerUnit = importer.spritePixelsPerUnit
        };
    }
}
```

## Notes

- This is the lightweight bridge getter. For the full importer settings (including `sRGB`, `wrapMode`, `filterMode`, `alphaSource`, etc.) use `texture_get_settings`.
- `maxSize` here reads `importer.maxTextureSize` (the global default) rather than a platform override.
