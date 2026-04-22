# audio_get_settings

Read the full importer settings for an audio asset.

**Skill ID:** `audio_get_settings`
**Source:** `AudioSkills.cs` — `AudioGetSettings`

## Signature

```
audio_get_settings(assetPath: string)
  → { success, path, forceToMono, loadInBackground, ambisonic,
      loadType, compressionFormat, quality, sampleRateSetting }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the audio file |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Audio/bgm.wav"; // Replace with target path

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }

        var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer == null)
            { result.SetResult(new { error = $"Not an audio file or asset not found: {assetPath}" }); return; }

        var defaultSettings = importer.defaultSampleSettings;

        { result.SetResult(new
        {
            success = true,
            path = assetPath,
            forceToMono = importer.forceToMono,
            loadInBackground = importer.loadInBackground,
            ambisonic = importer.ambisonic,
            loadType = defaultSettings.loadType.ToString(),
            compressionFormat = defaultSettings.compressionFormat.ToString(),
            quality = defaultSettings.quality,
            sampleRateSetting = defaultSettings.sampleRateSetting.ToString()
        }); return; }
    }
}
```

## Notes

- `quality` is returned as a `float` in the `0.0`–`1.0` range.
- Read-only; no reimport triggered.
- For the lightweight bridge getter, use `audio_get_import_settings`.
