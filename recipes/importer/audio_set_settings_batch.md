# audio_set_settings_batch

Apply audio importer settings to multiple audio assets in one call.

**Skill ID:** `audio_set_settings_batch`
**Source:** `AudioSkills.cs` — `AudioSetSettingsBatch`

## Signature

```
audio_set_settings_batch(items: string) → { results[], errors[] }
```

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `items` | string | yes | JSON array of item objects (see schema below) |

### Item Schema

Each object in the JSON array may contain:

| Field | Type | Description |
|-------|------|-------------|
| `assetPath` | string | Project-relative path (required per item) |
| `forceToMono` | bool | Downmix to mono |
| `loadInBackground` | bool | Background loading |
| `loadType` | string | `DecompressOnLoad`, `CompressedInMemory`, `Streaming` |
| `compressionFormat` | string | `PCM`, `Vorbis`, `ADPCM` |
| `quality` | float | 0.0–1.0 Vorbis quality |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## Unity_RunCommand Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchAudioItem
{
    public string assetPath;
    public bool? forceToMono;
    public bool? loadInBackground;
    public string loadType;
    public string compressionFormat;
    public float? quality;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchAudioItem { assetPath = "Assets/Audio/bgm.wav", loadType = "Streaming", compressionFormat = "Vorbis", quality = 0.7f },
            new _BatchAudioItem { assetPath = "Assets/Audio/sfx.wav", loadType = "DecompressOnLoad", forceToMono = true },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var importer = AssetImporter.GetAtPath(item.assetPath) as AudioImporter;
            if (importer == null) { results.Add(new { path = item.assetPath, success = false, error = "Not an audio file" }); failCount++; continue; }

            if (item.forceToMono.HasValue) importer.forceToMono = item.forceToMono.Value;
            if (item.loadInBackground.HasValue) importer.loadInBackground = item.loadInBackground.Value;

            var ss = importer.defaultSampleSettings;
            bool ssChanged = false;
            if (!string.IsNullOrEmpty(item.loadType) && System.Enum.TryParse<AudioClipLoadType>(item.loadType, true, out var lt))
            { ss.loadType = lt; ssChanged = true; }
            if (!string.IsNullOrEmpty(item.compressionFormat) && System.Enum.TryParse<AudioCompressionFormat>(item.compressionFormat, true, out var cf))
            { ss.compressionFormat = cf; ssChanged = true; }
            if (item.quality.HasValue)
            { ss.quality = Mathf.Clamp01(item.quality.Value); ssChanged = true; }
            if (ssChanged) importer.defaultSampleSettings = ss;

            importer.SaveAndReimport();
            results.Add(new { path = item.assetPath, success = true });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```

## Notes

- Unlike the texture batch, audio batch does not wrap in `StartAssetEditing`/`StopAssetEditing` because audio reimport is already incremental.
- Errors per item are captured; processing continues for remaining items.
