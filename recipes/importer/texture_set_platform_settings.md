# texture_set_platform_settings

Set per-platform texture import overrides (max size, format, compression quality).

**Skill ID:** `texture_set_platform_settings`
**Source:** `TextureSkills.cs` — `TextureSetPlatformSettings`

## Signature

```
texture_set_platform_settings(
  assetPath: string,
  platform: string,
  maxSize?: int,
  format?: string,
  compressionQuality?: int,
  overridden?: bool
) → { success, path, platform, maxSize, format }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the texture |
| `platform` | string | yes | `Standalone`, `iPhone`, `Android`, `WebGL` |
| `maxSize` | int | no | Max texture dimension for this platform |
| `format` | string | no | `TextureImporterFormat` value (e.g. `ASTC_6x6`, `ETC2_RGBA8`) |
| `compressionQuality` | int | no | 0–100 quality for compressed formats |
| `overridden` | bool | no | Whether platform override is active (default `true` when any value supplied) |

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
        int? maxSize = 512;
        string format = null;     // e.g. "ETC2_RGBA8"
        int? compressionQuality = null;
        bool? overridden = null;

        if (Validate.Required(assetPath, "assetPath") is object err) return err;
        if (Validate.Required(platform, "platform") is object err2) return err2;
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return new { error = $"Not a texture: {assetPath}" };

        var ps = importer.GetPlatformTextureSettings(platform);
        ps.overridden = overridden.HasValue ? overridden.Value : true;
        if (maxSize.HasValue) ps.maxTextureSize = maxSize.Value;
        if (!string.IsNullOrEmpty(format) && System.Enum.TryParse<TextureImporterFormat>(format, true, out var tf))
            ps.format = tf;
        if (compressionQuality.HasValue) ps.compressionQuality = compressionQuality.Value;

        importer.SetPlatformTextureSettings(ps);
        importer.SaveAndReimport();

        return new { success = true, path = assetPath, platform, maxSize = ps.maxTextureSize, format = ps.format.ToString() };
    }
}
```

## Notes

- Setting any value implicitly enables the override (`ps.overridden = true`) unless you explicitly pass `overridden=false`.
- Use `texture_get_platform_settings` to read the current override before changing it.
- Platform string must exactly match Unity's internal names: `Standalone`, `iPhone`, `Android`, `WebGL`.
