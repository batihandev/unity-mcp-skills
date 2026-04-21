# audio_set_import_settings

Alternative bridge setter for common audio importer fields.

**Skill ID:** `audio_set_import_settings`
**Source:** `AssetImportSkills.cs` — `AudioSetImportSettings`

## Signature

```
audio_set_import_settings(
  assetPath: string,
  forceToMono?: bool,
  loadInBackground?: bool,
  loadType?: string,
  compressionFormat?: string,
  quality?: int
) → { success, assetPath, forceToMono, loadType, compressionFormat }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the audio file |
| `forceToMono` | bool | no | Convert to mono |
| `loadInBackground` | bool | no | Background load |
| `loadType` | string | no | `DecompressOnLoad`, `CompressedInMemory`, `Streaming` |
| `compressionFormat` | string | no | `PCM`, `Vorbis`, `ADPCM` |
| `quality` | int | no | 0–100 (mapped to `0.0`–`1.0` internally) |

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Audio/bgm.wav"; // Replace
        bool? forceToMono = null;
        bool? loadInBackground = null;
        string loadType = "Streaming";        // DecompressOnLoad | CompressedInMemory | Streaming
        string compressionFormat = "Vorbis";  // PCM | Vorbis | ADPCM
        int? quality = 70;                    // 0 – 100 integer

        var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer == null) return new { error = $"Not an audio asset: {assetPath}" };

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        if (forceToMono.HasValue)     importer.forceToMono     = forceToMono.Value;
        if (loadInBackground.HasValue) importer.loadInBackground = loadInBackground.Value;

        var settings = importer.defaultSampleSettings;

        if (!string.IsNullOrEmpty(loadType) &&
            System.Enum.TryParse<AudioClipLoadType>(loadType, true, out var parsedLoadType))
            settings.loadType = parsedLoadType;

        if (!string.IsNullOrEmpty(compressionFormat) &&
            System.Enum.TryParse<AudioCompressionFormat>(compressionFormat, true, out var parsedCompression))
            settings.compressionFormat = parsedCompression;

        if (quality.HasValue) settings.quality = quality.Value / 100f;

        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();

        return new
        {
            success = true,
            assetPath,
            forceToMono = importer.forceToMono,
            loadType = settings.loadType.ToString(),
            compressionFormat = settings.compressionFormat.ToString()
        };
    }
}
```

## Notes

- `quality` here is an integer 0–100, unlike `audio_set_settings` which uses a `float` 0.0–1.0. Both map to the same internal field.
- `SaveAndReimport()` is always called.
