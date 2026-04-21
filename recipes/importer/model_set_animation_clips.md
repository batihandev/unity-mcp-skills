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

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Models/hero.fbx"; // Replace
        string clips = @"[
            { ""name"": ""Idle"",   ""firstFrame"": 0,  ""lastFrame"": 60,  ""loop"": true  },
            { ""name"": ""Run"",    ""firstFrame"": 61, ""lastFrame"": 90,  ""loop"": true  },
            { ""name"": ""Attack"", ""firstFrame"": 91, ""lastFrame"": 120, ""loop"": false }
        ]";

        if (Validate.Required(assetPath, "assetPath") is object err) return err;
        if (Validate.Required(clips, "clips") is object err2) return err2;

        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) return new { error = $"Not a model: {assetPath}" };

        var clipList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ClipDef>>(clips);
        if (clipList == null || clipList.Count == 0) return new { error = "No clips provided" };

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        importer.clipAnimations = clipList.Select(c => new ModelImporterClipAnimation
        {
            name = c.name,
            takeName = c.takeName ?? "Take 001",
            firstFrame = c.firstFrame,
            lastFrame = c.lastFrame,
            loopTime = c.loop
        }).ToArray();

        importer.SaveAndReimport();

        return new { success = true, path = assetPath, clipCount = clipList.Count };
    }
}
```

## Notes

- Replaces the entire `clipAnimations` array; include all clips you want to keep.
- After reimport, call `asset_reimport` to ensure the new clips are fully available in the scene.
- `takeName` should match a take present in the source file; defaults to `"Take 001"`.
