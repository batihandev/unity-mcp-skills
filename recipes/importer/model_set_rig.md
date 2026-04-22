# model_set_rig

Switch the rig/skeleton mode on a model and reimport.

**Skill ID:** `model_set_rig`
**Source:** `ModelSkills.cs` — `ModelSetRig`

## Signature

```
model_set_rig(assetPath: string, animationType: string, avatarSetup?: string)
  → { success, path, animationType }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the model file |
| `animationType` | string | yes | `None`, `Legacy`, `Generic`, `Humanoid` |
| `avatarSetup` | string | no | `NoAvatar`, `CreateFromThisModel`, `CopyFromOther` |

**Prerequisites:** [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

## Unity_RunCommand Template

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

        if (Validate.Required(assetPath, "assetPath") is object err) return err;
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) return new { error = $"Not a model: {assetPath}" };

        if (!System.Enum.TryParse<ModelImporterAnimationType>(animationType, true, out var at))
            return new { error = $"Invalid animationType: {animationType}" };

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        importer.animationType = at;

        if (!string.IsNullOrEmpty(avatarSetup) &&
            System.Enum.TryParse<ModelImporterAvatarSetup>(avatarSetup, true, out var avs))
            importer.avatarSetup = avs;

        importer.SaveAndReimport();

        return new { success = true, path = assetPath, animationType = at.ToString() };
    }
}
```

## Notes

- Switching to `Humanoid` triggers Unity's avatar configuration. Use `asset_reimport` afterwards to fully refresh.
- `avatarSetup` is optional; when omitted the importer's existing setting is preserved.
- After rig change, any existing `Animator` components referencing the avatar may need to be re-assigned.
