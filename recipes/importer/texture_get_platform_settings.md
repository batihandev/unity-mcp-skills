# texture_get_platform_settings

Read the per-platform texture import override for a specific platform.

**Skill ID:** `texture_get_platform_settings`
**Source:** `TextureSkills.cs` — `TextureGetPlatformSettings`

## Signature

```
texture_get_platform_settings(assetPath: string, platform: string)
  → { success, path, platform, overridden, maxTextureSize, format, compressionQuality }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the texture |
| `platform` | string | yes | `Standalone`, `iPhone`, `Android`, `WebGL` |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

## Unity_RunCommand Template

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
- Read-only call; no reimport is triggered.
