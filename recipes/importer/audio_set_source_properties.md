# audio_set_source_properties

Update fields on an AudioSource component attached to a scene GameObject.

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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

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
        if (error != null) { result.SetResult(error); return; }

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

        { result.SetResult(new { success = true, gameObject = source.gameObject.name }); return; }
    }
}
```

## Notes

- Uses `Undo.RecordObject` so changes are undo-able in the Editor.
- Only supply parameters you want to change; unspecified parameters are left untouched.
