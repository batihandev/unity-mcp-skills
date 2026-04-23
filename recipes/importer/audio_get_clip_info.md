# audio_get_clip_info

Inspect length, channels, frequency, samples, and load state of an audio clip.

## Signature

```
audio_get_clip_info(assetPath: string)
  → { success, name, path, length, channels, frequency, samples,
      loadType, loadState, ambisonic }
```

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Audio/bgm.wav"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (clip == null) { result.SetResult(new { error = $"AudioClip not found: {assetPath}" }); return; }

        { result.SetResult(new
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
        }); return; }
    }
}
```

## Notes

- `length` is in seconds; `frequency` is in Hz.
- `loadState` reflects the runtime load state at the time of the call in the Editor.
- For importer-level settings, use `audio_get_settings`.
