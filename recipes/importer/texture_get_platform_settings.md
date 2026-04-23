# texture_get_platform_settings

Read the per-platform texture import override for a specific platform.

## Signature

```
texture_get_platform_settings(assetPath: string, platform: string)
  → { success, path, platform, overridden, maxTextureSize, format, compressionQuality }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/hero.png"; // Replace
        string platform = "Android"; // Standalone | iPhone | Android | WebGL

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }
        if (Validate.Required(platform, "platform") is object err2) { result.SetResult(err2); return; }
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) { result.SetResult(new { error = $"Not a texture: {assetPath}" }); return; }

        var ps = importer.GetPlatformTextureSettings(platform);
        { result.SetResult(new
        {
            success = true,
            path = assetPath,
            platform,
            overridden = ps.overridden,
            maxTextureSize = ps.maxTextureSize,
            format = ps.format.ToString(),
            compressionQuality = ps.compressionQuality
        }); return; }
    }
}
```

## Notes
- If no override is active, `overridden` will be `false` and the returned values reflect Unity's fallback defaults.

