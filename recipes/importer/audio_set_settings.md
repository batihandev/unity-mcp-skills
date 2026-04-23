# audio_set_settings

Set importer settings on an audio asset and reimport.

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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

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

        if (Validate.Required(assetPath, "assetPath") is object err) { result.SetResult(err); return; }

        var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer == null)
            { result.SetResult(new { error = $"Not an audio file or asset not found: {assetPath}" }); return; }

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
                { result.SetResult(new { error = $"Invalid loadType: {loadType}. Valid: DecompressOnLoad, CompressedInMemory, Streaming" }); return; }
        }

        if (!string.IsNullOrEmpty(compressionFormat))
        {
            if (System.Enum.TryParse<AudioCompressionFormat>(compressionFormat, true, out var cf))
            { sampleSettings.compressionFormat = cf; changes.Add($"compressionFormat={cf}"); sampleSettingsChanged = true; }
            else
                { result.SetResult(new { error = $"Invalid compressionFormat: {compressionFormat}. Valid: PCM, Vorbis, ADPCM" }); return; }
        }

        if (quality.HasValue)
        { sampleSettings.quality = Mathf.Clamp01(quality.Value); changes.Add($"quality={sampleSettings.quality}"); sampleSettingsChanged = true; }

        if (!string.IsNullOrEmpty(sampleRateSetting) && System.Enum.TryParse<AudioSampleRateSetting>(sampleRateSetting, true, out var srs))
        { sampleSettings.sampleRateSetting = srs; changes.Add($"sampleRateSetting={srs}"); sampleSettingsChanged = true; }

        if (sampleSettingsChanged) importer.defaultSampleSettings = sampleSettings;

        importer.SaveAndReimport();

        { result.SetResult(new { success = true, path = assetPath, changesApplied = changes.Count, changes }); return; }
    }
}
```

## Notes

- `quality` for Vorbis is clamped to `0.0`–`1.0`. The `audio_set_import_settings` bridge accepts 0–100 integers (maps to `0.0`–`1.0` internally).
- `SaveAndReimport()` is always called after any change.
