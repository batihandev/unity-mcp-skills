# model_get_rig_info

Read the rig configuration (animation type, avatar setup) of a model.

**Skill ID:** `model_get_rig_info`
**Source:** `ModelSkills.cs` — `ModelGetRigInfo`

## Signature

```
model_get_rig_info(assetPath: string)
  → { success, path, animationType, avatarSetup, sourceAvatar, optimizeGameObjects, isHuman }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the model file |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) return err;
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) return new { error = $"Not a model: {assetPath}" };

        return new
        {
            success = true,
            path = assetPath,
            animationType = importer.animationType.ToString(),
            avatarSetup = importer.avatarSetup.ToString(),
            sourceAvatar = importer.sourceAvatar != null ? importer.sourceAvatar.name : "null",
            optimizeGameObjects = importer.optimizeGameObjects,
            isHuman = importer.animationType == ModelImporterAnimationType.Human
        };
    }
}
```

## Notes

- `isHuman` is a convenience bool (`true` when `animationType == Humanoid`).
- Read-only; no reimport triggered.
- Use `model_set_rig` to change the rig configuration.
