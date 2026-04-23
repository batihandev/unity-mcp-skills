# sprite_set_import_settings

Sprite importer bridge — set sprite mode, pixels-per-unit, packing tag, and pivot.

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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

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
        if (importer == null) { result.SetResult(new { error = $"Not a texture: {assetPath}" }); return; }

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

        { result.SetResult(new
        {
            success = true,
            assetPath,
            spriteMode = importer.spriteImportMode.ToString(),
            pixelsPerUnit = importer.spritePixelsPerUnit
        }); return; }
    }
}
```

## Notes

- Automatically forces `textureType = Sprite`.
- `pivotX` / `pivotY` are passed as strings and parsed with `InvariantCulture` to avoid locale-specific decimal separators.
- For just PPU and sprite mode without pivot/packing tag, `texture_set_sprite_settings` is simpler.
