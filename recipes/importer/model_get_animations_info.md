# model_get_animations_info

List animation clips and frame rates embedded in a model file.

**Skill ID:** `model_get_animations_info`
**Source:** `ModelSkills.cs` — `ModelGetAnimationsInfo`

## Signature

```
model_get_animations_info(assetPath: string)
  → { success, path, importAnimation, clipCount,
      clips[{ name, length, frameRate, wrapMode, isLooping }],
      clipDefinitions[{ name, firstFrame, lastFrame, loop }] }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the model file |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) { result.SetResult(new { error = $"Not a model: {assetPath}" }); return; }

        var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        var clips = allAssets.OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .Select(c => new
            {
                name = c.name,
                length = c.length,
                frameRate = c.frameRate,
                wrapMode = c.wrapMode.ToString(),
                isLooping = c.isLooping
            }).ToArray();

        var importedClips = importer.clipAnimations;
        var clipDefs = importedClips != null
            ? importedClips.Select(c => new
              {
                  name = c.name,
                  firstFrame = c.firstFrame,
                  lastFrame = c.lastFrame,
                  loop = c.loopTime
              }).ToArray()
            : null;

        { result.SetResult(new
        {
            success = true,
            path = assetPath,
            importAnimation = importer.importAnimation,
            clipCount = clips.Length,
            clips,
            clipDefinitions = clipDefs
        }); return; }
    }
}
```

## Notes

- Preview clips (prefixed `__preview__`) are filtered out.
- `clipDefinitions` reflects the clip split definitions set in the importer (may differ from the baked sub-assets).
- Use `model_set_animation_clips` to configure clip splits.
