# model_set_animation_clips

Configure animation clip splits on a model importer.

**Skill ID:** `model_set_animation_clips`
**Source:** `ModelSkills.cs` — `ModelSetAnimationClips`

## Signature

```
model_set_animation_clips(assetPath: string, clips: string)
  → { success, path, clipCount }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the model file |
| `clips` | string | yes | JSON array of clip definition objects |

### Clip Object Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | yes | Name for the animation clip |
| `firstFrame` | int | yes | Start frame |
| `lastFrame` | int | yes | End frame |
| `loop` | bool | no | Loop the clip |
| `takeName` | string | no | Take name in file (default `"Take 001"`) |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.SnapshotObject`

## Unity_RunCommand Template

The upstream JSON-string form is replaced by a typed `_AnimClipDef[]` — agents pass a native C# array.

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal sealed class _AnimClipDef
{
    public string name;
    public int firstFrame;
    public int lastFrame;
    public bool loop;
    public string takeName;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx";
        var clips = new[]
        {
            new _AnimClipDef { name = "Idle",   firstFrame = 0,   lastFrame = 60,  loop = true },
            new _AnimClipDef { name = "Run",    firstFrame = 61,  lastFrame = 90,  loop = true },
            new _AnimClipDef { name = "Attack", firstFrame = 91,  lastFrame = 120, loop = false },
        };

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }

        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) { result.SetResult(new { error = $"Not a model: {assetPath}" }); return; }
        if (clips == null || clips.Length == 0) { result.SetResult(new { error = "No clips provided" }); return; }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        importer.clipAnimations = clips.Select(c => new ModelImporterClipAnimation
        {
            name = c.name,
            takeName = c.takeName ?? "Take 001",
            firstFrame = c.firstFrame,
            lastFrame = c.lastFrame,
            loopTime = c.loop
        }).ToArray();

        importer.SaveAndReimport();

        result.SetResult(new { success = true, path = assetPath, clipCount = clips.Length });
    }
}
```

## Notes

- Replaces the entire `clipAnimations` array; include all clips you want to keep.
- After reimport, call `asset_reimport` to ensure the new clips are fully available in the scene.
- `takeName` should match a take present in the source file; defaults to `"Take 001"`.
