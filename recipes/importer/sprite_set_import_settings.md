# sprite_set_import_settings

Sprite importer bridge — set sprite mode, pixels-per-unit, packing tag, and pivot.

**Skill ID:** `sprite_set_import_settings`
**Source:** `AssetImportSkills.cs` — `SpriteSetImportSettings`

## Signature

```
sprite_set_import_settings(
  assetPath: string,
  spriteMode?: string,
  pixelsPerUnit?: float,
  packingTag?: string,
  pivotX?: string,
  pivotY?: string
) → { success, assetPath, spriteMode, pixelsPerUnit }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the texture |
| `spriteMode` | string | no | `Single`, `Multiple`, `Polygon` |
| `pixelsPerUnit` | float | no | Pixels per unit |
| `packingTag` | string | no | Sprite atlas packing tag |
| `pivotX` | string | no | Pivot X as float string (e.g. `"0.5"`) |
| `pivotY` | string | no | Pivot Y as float string (e.g. `"0.5"`) |

**Prerequisites:** [`workflow_manager`](../_shared/workflow_manager.md)

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Sprites/hero.png"; // Replace
        string spriteMode = "Single";   // Single | Multiple | Polygon
        float? pixelsPerUnit = 100f;
        string packingTag = null;       // Optional sprite atlas tag
        string pivotX = "0.5";          // Pivot X (0.0 – 1.0)
        string pivotY = "0.5";          // Pivot Y (0.0 – 1.0)

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return new { error = $"Not a texture: {assetPath}" };

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        importer.textureType = TextureImporterType.Sprite;

        if (!string.IsNullOrEmpty(spriteMode))
        {
            switch (spriteMode.ToLower())
            {
                case "single":   importer.spriteImportMode = SpriteImportMode.Single;   break;
                case "multiple": importer.spriteImportMode = SpriteImportMode.Multiple; break;
                case "polygon":  importer.spriteImportMode = SpriteImportMode.Polygon;  break;
            }
        }

        if (pixelsPerUnit.HasValue) importer.spritePixelsPerUnit = pixelsPerUnit.Value;
        if (!string.IsNullOrEmpty(packingTag)) importer.spritePackingTag = packingTag;
        if (pivotX != null && pivotY != null)
        {
            importer.spritePivot = new Vector2(
                float.Parse(pivotX, System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(pivotY, System.Globalization.CultureInfo.InvariantCulture));
        }

        importer.SaveAndReimport();

        return new
        {
            success = true,
            assetPath,
            spriteMode = importer.spriteImportMode.ToString(),
            pixelsPerUnit = importer.spritePixelsPerUnit
        };
    }
}
```

## Notes

- Automatically forces `textureType = Sprite`.
- `pivotX` / `pivotY` are passed as strings and parsed with `InvariantCulture` to avoid locale-specific decimal separators.
- For just PPU and sprite mode without pivot/packing tag, `texture_set_sprite_settings` is simpler.
