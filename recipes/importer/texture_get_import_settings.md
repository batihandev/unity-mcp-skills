# texture_get_import_settings

Read a minimal set of texture importer settings (bridge getter).

## Signature

```
texture_get_import_settings(assetPath: string)
  → { success, assetPath, textureType, maxSize, compression, readable, mipmaps, spriteMode, pixelsPerUnit }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/hero.png"; // Replace with target path

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) { result.SetResult(new { error = $"Not a texture: {assetPath}" }); return; }

        { result.SetResult(new
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
        }); return; }
    }
}
```

## Notes

- This is the lightweight bridge getter. For the full importer settings (including `sRGB`, `wrapMode`, `filterMode`, `alphaSource`, etc.) use `texture_get_settings`.
- `maxSize` here reads `importer.maxTextureSize` (the global default) rather than a platform override.
