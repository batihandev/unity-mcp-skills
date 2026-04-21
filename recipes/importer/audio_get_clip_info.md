# audio_get_clip_info

Inspect length, channels, frequency, samples, and load state of an audio clip.

**Skill ID:** `audio_get_clip_info`
**Source:** `AudioSkills.cs` — `AudioGetClipInfo`

## Signature

```
audio_get_clip_info(assetPath: string)
  → { success, name, path, length, channels, frequency, samples,
      loadType, loadState, ambisonic }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the audio asset |

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Audio/bgm.wav"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) return err;
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (clip == null) return new { error = $"AudioClip not found: {assetPath}" };

        return new
        {
            success = true,
            name = clip.name,
            path = assetPath,
            length = clip.length,
            channels = clip.channels,
            frequency = clip.frequency,
            samples = clip.samples,
            loadType = clip.loadType.ToString(),
            loadState = clip.loadState.ToString(),
            ambisonic = clip.ambisonic
        };
    }
}
```

## Notes

- `length` is in seconds; `frequency` is in Hz.
- `loadState` reflects the runtime load state at the time of the call in the Editor.
- For importer-level settings, use `audio_get_settings`.
