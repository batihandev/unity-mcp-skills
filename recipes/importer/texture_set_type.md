# texture_set_type

Switch the texture type on a texture asset and reimport.

## Signature

```
texture_set_type(assetPath: string, textureType: string)
  → { success, path, textureType }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/hero.png"; // Replace
        string textureType = "Sprite"; // Replace with desired type

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) { result.SetResult(new { error = $"Not a texture: {assetPath}" }); return; }
        if (!System.Enum.TryParse<TextureImporterType>(textureType.Replace(" ", ""), true, out var tt))
            { result.SetResult(new { error = $"Invalid textureType: {textureType}" }); return; }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        importer.textureType = tt;
        importer.SaveAndReimport();

        { result.SetResult(new { success = true, path = assetPath, textureType = tt.ToString() }); return; }
    }
}
```

## Notes

- Changing texture type can reset or expose additional importer fields (e.g. PPU for Sprite, wrap mode for Cookie).
- Use `texture_set_settings` if you also need to change other fields at the same time.
