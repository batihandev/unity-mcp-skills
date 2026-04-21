# audio_set_settings

Set importer settings on an audio asset and reimport.

**Skill ID:** `audio_set_settings`
**Source:** `AudioSkills.cs` — `AudioSetSettings`

## Signature

```
audio_set_settings(
  assetPath: string,
  forceToMono?: bool,
  loadInBackground?: bool,
  ambisonic?: bool,
  loadType?: string,
  compressionFormat?: string,
  quality?: float,
  sampleRateSetting?: string
) → { success, path, changesApplied, changes }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the audio file |
| `forceToMono` | bool | no | Downmix to mono |
| `loadInBackground` | bool | no | Load clip in background thread |
| `ambisonic` | bool | no | Mark as ambisonic audio |
| `loadType` | string | no | `DecompressOnLoad`, `CompressedInMemory`, `Streaming` |
| `compressionFormat` | string | no | `PCM`, `Vorbis`, `ADPCM` |
| `quality` | float | no | 0.0–1.0 quality for Vorbis compression |
| `sampleRateSetting` | string | no | `PreserveSampleRate`, `OptimizeSampleRate`, `OverrideSampleRate` |

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Audio/bgm.wav"; // Replace
        bool? forceToMono = null;
        bool? loadInBackground = null;
        bool? ambisonic = null;
        string loadType = "Streaming";          // DecompressOnLoad | CompressedInMemory | Streaming
        string compressionFormat = "Vorbis";    // PCM | Vorbis | ADPCM
        float? quality = 0.7f;                  // 0.0 – 1.0
        string sampleRateSetting = null;

        if (Validate.Required(assetPath, "assetPath") is object err) return err;

        var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer == null)
            return new { error = $"Not an audio file or asset not found: {assetPath}" };

        var changes = new List<string>();

        if (forceToMono.HasValue)    { importer.forceToMono    = forceToMono.Value;    changes.Add($"forceToMono={forceToMono.Value}"); }
        if (loadInBackground.HasValue){ importer.loadInBackground = loadInBackground.Value; changes.Add($"loadInBackground={loadInBackground.Value}"); }
        if (ambisonic.HasValue)      { importer.ambisonic      = ambisonic.Value;      changes.Add($"ambisonic={ambisonic.Value}"); }

        var sampleSettings = importer.defaultSampleSettings;
        bool sampleSettingsChanged = false;

        if (!string.IsNullOrEmpty(loadType))
        {
            if (System.Enum.TryParse<AudioClipLoadType>(loadType, true, out var lt))
            { sampleSettings.loadType = lt; changes.Add($"loadType={lt}"); sampleSettingsChanged = true; }
            else
                return new { error = $"Invalid loadType: {loadType}. Valid: DecompressOnLoad, CompressedInMemory, Streaming" };
        }

        if (!string.IsNullOrEmpty(compressionFormat))
        {
            if (System.Enum.TryParse<AudioCompressionFormat>(compressionFormat, true, out var cf))
            { sampleSettings.compressionFormat = cf; changes.Add($"compressionFormat={cf}"); sampleSettingsChanged = true; }
            else
                return new { error = $"Invalid compressionFormat: {compressionFormat}. Valid: PCM, Vorbis, ADPCM" };
        }

        if (quality.HasValue)
        { sampleSettings.quality = Mathf.Clamp01(quality.Value); changes.Add($"quality={sampleSettings.quality}"); sampleSettingsChanged = true; }

        if (!string.IsNullOrEmpty(sampleRateSetting) && System.Enum.TryParse<AudioSampleRateSetting>(sampleRateSetting, true, out var srs))
        { sampleSettings.sampleRateSetting = srs; changes.Add($"sampleRateSetting={srs}"); sampleSettingsChanged = true; }

        if (sampleSettingsChanged) importer.defaultSampleSettings = sampleSettings;

        importer.SaveAndReimport();

        return new { success = true, path = assetPath, changesApplied = changes.Count, changes };
    }
}
```

## Notes

- `quality` for Vorbis is clamped to `0.0`–`1.0`. The `audio_set_import_settings` bridge accepts 0–100 integers (maps to `0.0`–`1.0` internally).
- `SaveAndReimport()` is always called after any change.
