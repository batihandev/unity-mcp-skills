# audio_get_import_settings

Read a minimal set of audio importer settings (bridge getter).

**Skill ID:** `audio_get_import_settings`
**Source:** `AssetImportSkills.cs` — `AudioGetImportSettings`

## Signature

```
audio_get_import_settings(assetPath: string)
  → { success, assetPath, forceToMono, loadInBackground, loadType, compressionFormat, quality }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | yes | Project-relative path to the audio file |

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Audio/bgm.wav"; // Replace with target path

        var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer == null) return new { error = $"Not an audio asset: {assetPath}" };

        var settings = importer.defaultSampleSettings;
        return new
        {
            success = true,
            assetPath,
            forceToMono = importer.forceToMono,
            loadInBackground = importer.loadInBackground,
            loadType = settings.loadType.ToString(),
            compressionFormat = settings.compressionFormat.ToString(),
            quality = settings.quality
        };
    }
}
```

## Notes

- This is the lightweight bridge getter. For the full importer settings including `ambisonic` and `sampleRateSetting`, use `audio_get_settings`.
- `quality` is returned as a `float` in `0.0`–`1.0` range.
- Read-only; no reimport triggered.
