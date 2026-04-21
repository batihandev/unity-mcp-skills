# texture_set_type

Switch the texture type on a texture asset and reimport.

**Skill ID:** `texture_set_type`
**Source:** `TextureSkills.cs` — `TextureSetType`

## Signature

```
texture_set_type(assetPath: string, textureType: string)
  → { success, path, textureType }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the texture |
| `textureType` | string | yes | `Default`, `NormalMap`, `Sprite`, `EditorGUI`, `Cursor`, `Cookie`, `Lightmap`, `SingleChannel` |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/hero.png"; // Replace
        string textureType = "Sprite"; // Replace with desired type

        if (Validate.Required(assetPath, "assetPath") is object err) return err;
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return new { error = $"Not a texture: {assetPath}" };
        if (!System.Enum.TryParse<TextureImporterType>(textureType.Replace(" ", ""), true, out var tt))
            return new { error = $"Invalid textureType: {textureType}" };

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        importer.textureType = tt;
        importer.SaveAndReimport();

        return new { success = true, path = assetPath, textureType = tt.ToString() };
    }
}
```

## Notes

- Changing texture type can reset or expose additional importer fields (e.g. PPU for Sprite, wrap mode for Cookie).
- Use `texture_set_settings` if you also need to change other fields at the same time.
