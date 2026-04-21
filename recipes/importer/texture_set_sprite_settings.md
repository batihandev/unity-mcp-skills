# texture_set_sprite_settings

Configure sprite-specific importer knobs (pixels-per-unit, sprite mode).

**Skill ID:** `texture_set_sprite_settings`
**Source:** `TextureSkills.cs` — `TextureSetSpriteSettings`

## Signature

```
texture_set_sprite_settings(assetPath: string, pixelsPerUnit?: float, spriteMode?: string)
  → { success, path, pixelsPerUnit, spriteMode }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the texture |
| `pixelsPerUnit` | float | no | How many pixels correspond to one Unity unit |
| `spriteMode` | string | no | `Single`, `Multiple`, `Polygon` |

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Sprites/hero.png"; // Replace
        float? pixelsPerUnit = 100f;
        string spriteMode = "Single"; // Single | Multiple | Polygon

        if (Validate.Required(assetPath, "assetPath") is object err) return err;
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return new { error = $"Not a texture: {assetPath}" };

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        if (pixelsPerUnit.HasValue) importer.spritePixelsPerUnit = pixelsPerUnit.Value;
        if (!string.IsNullOrEmpty(spriteMode) && System.Enum.TryParse<SpriteImportMode>(spriteMode, true, out var sm))
            importer.spriteImportMode = sm;

        importer.SaveAndReimport();

        return new
        {
            success = true,
            path = assetPath,
            pixelsPerUnit = importer.spritePixelsPerUnit,
            spriteMode = importer.spriteImportMode.ToString()
        };
    }
}
```

## Notes

- The texture type does not need to be `Sprite` before calling this, but it should be for the values to have meaningful effect.
- For setting PPU alongside pivot or packing tag, use `sprite_set_import_settings`.
