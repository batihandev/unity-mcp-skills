# texture_set_settings

Set one or more importer settings on a texture asset and reimport it.

**Skill ID:** `texture_set_settings`
**Source:** `TextureSkills.cs` — `TextureSetSettings`

## Signature

```
texture_set_settings(
  assetPath: string,
  textureType?: string,
  maxSize?: int,
  filterMode?: string,
  compression?: string,
  mipmapEnabled?: bool,
  sRGB?: bool,
  readable?: bool,
  alphaIsTransparency?: bool,
  spritePixelsPerUnit?: float,
  wrapMode?: string,
  npotScale?: string
) → { success, path, changesApplied, changes }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the texture |
| `textureType` | string | no | `Default`, `NormalMap`, `Sprite`, `EditorGUI`, `Cursor`, `Cookie`, `Lightmap`, `SingleChannel` |
| `maxSize` | int | no | Max texture dimension (32–8192) |
| `filterMode` | string | no | `Point`, `Bilinear`, `Trilinear` |
| `compression` | string | no | `None`, `LowQuality`, `NormalQuality`, `HighQuality` |
| `mipmapEnabled` | bool | no | Generate mipmaps |
| `sRGB` | bool | no | Treat as sRGB colour texture |
| `readable` | bool | no | Enable CPU read access (`isReadable`) |
| `alphaIsTransparency` | bool | no | Dilate colour channels at alpha edges |
| `spritePixelsPerUnit` | float | no | Pixels-per-unit for Sprite type |
| `wrapMode` | string | no | `Repeat`, `Clamp`, `Mirror`, `MirrorOnce` |
| `npotScale` | string | no | NPOT scaling mode (`None`, `ToNearest`, `ToLarger`, `ToSmaller`) |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/hero.png"; // Replace
        string textureType = null;       // e.g. "Sprite"
        int? maxSize = null;             // e.g. 1024
        string filterMode = null;        // e.g. "Point"
        string compression = null;       // e.g. "NormalQuality"
        bool? mipmapEnabled = null;
        bool? sRGB = null;
        bool? readable = null;
        bool? alphaIsTransparency = null;
        float? spritePixelsPerUnit = null;
        string wrapMode = null;
        string npotScale = null;

        if (Validate.Required(assetPath, "assetPath") is object err) return err;

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return new { error = $"Not a texture or asset not found: {assetPath}" };

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        var changes = new List<string>();

        if (!string.IsNullOrEmpty(textureType))
        {
            if (System.Enum.TryParse<TextureImporterType>(textureType.Replace(" ", ""), true, out var tt))
            {
                importer.textureType = tt;
                changes.Add($"textureType={tt}");
            }
            else
                return new { error = $"Invalid textureType: {textureType}. Valid: Default, NormalMap, Sprite, EditorGUI, Cursor, Cookie, Lightmap, SingleChannel" };
        }

        if (!string.IsNullOrEmpty(filterMode) && System.Enum.TryParse<FilterMode>(filterMode, true, out var fm))
        {
            importer.filterMode = fm;
            changes.Add($"filterMode={fm}");
        }

        if (!string.IsNullOrEmpty(wrapMode) && System.Enum.TryParse<TextureWrapMode>(wrapMode, true, out var wm))
        {
            importer.wrapMode = wm;
            changes.Add($"wrapMode={wm}");
        }

        if (!string.IsNullOrEmpty(npotScale) && System.Enum.TryParse<TextureImporterNPOTScale>(npotScale, true, out var ns))
        {
            importer.npotScale = ns;
            changes.Add($"npotScale={ns}");
        }

        if (mipmapEnabled.HasValue) { importer.mipmapEnabled = mipmapEnabled.Value; changes.Add($"mipmapEnabled={mipmapEnabled.Value}"); }
        if (sRGB.HasValue) { importer.sRGBTexture = sRGB.Value; changes.Add($"sRGB={sRGB.Value}"); }
        if (readable.HasValue) { importer.isReadable = readable.Value; changes.Add($"readable={readable.Value}"); }
        if (alphaIsTransparency.HasValue) { importer.alphaIsTransparency = alphaIsTransparency.Value; changes.Add($"alphaIsTransparency={alphaIsTransparency.Value}"); }
        if (spritePixelsPerUnit.HasValue) { importer.spritePixelsPerUnit = spritePixelsPerUnit.Value; changes.Add($"spritePixelsPerUnit={spritePixelsPerUnit.Value}"); }

        if (maxSize.HasValue || !string.IsNullOrEmpty(compression))
        {
            var platformSettings = importer.GetDefaultPlatformTextureSettings();
            if (maxSize.HasValue) { platformSettings.maxTextureSize = maxSize.Value; changes.Add($"maxSize={maxSize.Value}"); }
            if (!string.IsNullOrEmpty(compression) && System.Enum.TryParse<TextureImporterCompression>(compression, true, out var tc))
            {
                platformSettings.textureCompression = tc;
                changes.Add($"compression={tc}");
            }
            importer.SetPlatformTextureSettings(platformSettings);
        }

        importer.SaveAndReimport();

        return new { success = true, path = assetPath, changesApplied = changes.Count, changes };
    }
}
```

## Notes

- `maxSize` and `compression` apply to the **default** platform settings. For per-platform overrides use `texture_set_platform_settings`.
- `SaveAndReimport()` is always called so changes take effect immediately.
- Only supply parameters you want to change; unspecified parameters are left untouched.
