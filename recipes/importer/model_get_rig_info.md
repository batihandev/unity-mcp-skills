# model_get_rig_info

Read the rig configuration (animation type, avatar setup) of a model.

## Signature

```
model_get_rig_info(assetPath: string)
  → { success, path, animationType, avatarSetup, sourceAvatar, optimizeGameObjects, isHuman }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) { result.SetResult(new { error = $"Not a model: {assetPath}" }); return; }

        { result.SetResult(new
        {
            success = true,
            path = assetPath,
            animationType = importer.animationType.ToString(),
            avatarSetup = importer.avatarSetup.ToString(),
            sourceAvatar = importer.sourceAvatar != null ? importer.sourceAvatar.name : "null",
            optimizeGameObjects = importer.optimizeGameObjects,
            isHuman = importer.animationType == ModelImporterAnimationType.Human
        }); return; }
    }
}
```

## Notes
- Use `model_set_rig` to change the rig configuration.

