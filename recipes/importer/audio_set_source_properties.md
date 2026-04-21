# audio_set_source_properties

Update fields on an AudioSource component attached to a scene GameObject.

**Skill ID:** `audio_set_source_properties`
**Source:** `AudioSkills.cs` — `AudioSetSourceProperties`

## Signature

```
audio_set_source_properties(
  name?: string,
  instanceId?: int,
  path?: string,
  clipPath?: string,
  volume?: float,
  pitch?: float,
  loop?: bool,
  playOnAwake?: bool,
  mute?: bool,
  spatialBlend?: float,
  priority?: int
) → { success, gameObject }
```

## Parameters

Target resolution — provide at least one of `name`, `instanceId`, or `path`:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | no | GameObject name |
| `instanceId` | int | no | GameObject instance ID |
| `path` | string | no | Hierarchy path |
| `clipPath` | string | no | Asset path to new AudioClip |
| `volume` | float | no | 0.0–1.0 |
| `pitch` | float | no | Pitch multiplier |
| `loop` | bool | no | Loop the clip |
| `playOnAwake` | bool | no | Auto-play on scene start |
| `mute` | bool | no | Mute the source |
| `spatialBlend` | float | no | 0 = 2D, 1 = 3D |
| `priority` | int | no | 0–256 (0 = highest) |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Player"; // Replace
        int instanceId = 0;
        string path = null;
        string clipPath = null;
        float? volume = 0.8f;
        float? pitch = null;
        bool? loop = true;
        bool? playOnAwake = null;
        bool? mute = null;
        float? spatialBlend = null;
        int? priority = null;

        var (source, error) = GameObjectFinder.FindComponentOrError<AudioSource>(name, instanceId, path);
        if (error != null) return error;

        Undo.RecordObject(source, "Set AudioSource Properties");

        if (!string.IsNullOrEmpty(clipPath))
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip != null) source.clip = clip;
        }

        if (volume.HasValue)      source.volume      = volume.Value;
        if (pitch.HasValue)       source.pitch       = pitch.Value;
        if (loop.HasValue)        source.loop        = loop.Value;
        if (playOnAwake.HasValue) source.playOnAwake = playOnAwake.Value;
        if (mute.HasValue)        source.mute        = mute.Value;
        if (spatialBlend.HasValue)source.spatialBlend = spatialBlend.Value;
        if (priority.HasValue)    source.priority    = priority.Value;

        return new { success = true, gameObject = source.gameObject.name };
    }
}
```

## Notes

- Uses `Undo.RecordObject` so changes are undo-able in the Editor.
- Only supply parameters you want to change; unspecified parameters are left untouched.
