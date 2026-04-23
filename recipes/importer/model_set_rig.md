# model_set_rig

Switch the rig/skeleton mode on a model and reimport.

## Signature

```
model_set_rig(assetPath: string, animationType: string, avatarSetup?: string)
  → { success, path, animationType }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx"; // Replace
        string animationType = "Humanoid";            // None | Legacy | Generic | Humanoid
        string avatarSetup = null;                    // Optional: NoAvatar | CreateFromThisModel | CopyFromOther

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) { result.SetResult(new { error = $"Not a model: {assetPath}" }); return; }

        if (!System.Enum.TryParse<ModelImporterAnimationType>(animationType, true, out var at))
            { result.SetResult(new { error = $"Invalid animationType: {animationType}" }); return; }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        importer.animationType = at;

        if (!string.IsNullOrEmpty(avatarSetup) &&
            System.Enum.TryParse<ModelImporterAvatarSetup>(avatarSetup, true, out var avs))
            importer.avatarSetup = avs;

        importer.SaveAndReimport();

        { result.SetResult(new { success = true, path = assetPath, animationType = at.ToString() }); return; }
    }
}
```

## Notes

- Switching to `Humanoid` triggers Unity's avatar configuration. Use `asset_reimport` afterwards to fully refresh.
- `avatarSetup` is optional; when omitted the importer's existing setting is preserved.
- After rig change, any existing `Animator` components referencing the avatar may need to be re-assigned.
