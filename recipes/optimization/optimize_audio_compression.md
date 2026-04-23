# optimize_audio_compression

Batch-apply audio compression settings to all `AudioClip` assets matching an optional filter. Uses `AssetDatabase.StartAssetEditing` / `StopAssetEditing` for efficiency. Skips clips already using the target settings.

**Signature:** `OptimizeAudioCompression(string compressionFormat = "Vorbis", string loadType = "CompressedInMemory", float quality = 0.5f, string filter = "")`

**Returns:** `{ success, count, compressionFormat, loadType, modified }`

- `compressionFormat` — `PCM` | `Vorbis` | `ADPCM` (case-insensitive)
- `loadType` — `DecompressOnLoad` | `CompressedInMemory` | `Streaming` (case-insensitive)
- `quality` — clamped to `[0, 1]`
- `modified` — array of `{ path }` for each reimported clip

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string compressionFormat = "Vorbis";          // PCM / Vorbis / ADPCM
        string loadType = "CompressedInMemory";       // DecompressOnLoad / CompressedInMemory / Streaming
        float quality = 0.5f;                         // 0.0–1.0
        string filter = "";                           // Extra AssetDatabase filter

        if (!System.Enum.TryParse<AudioCompressionFormat>(compressionFormat, true, out var cf))
        {
            result.SetResult(new { error = $"Invalid compressionFormat: {compressionFormat}" });
            return;
        }
        if (!System.Enum.TryParse<AudioClipLoadType>(loadType, true, out var lt))
        {
            result.SetResult(new { error = $"Invalid loadType: {loadType}" });
            return;
        }

        var guids = AssetDatabase.FindAssets("t:AudioClip " + filter);
        var modified = new List<object>();

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var guid in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(p) as AudioImporter;
                if (importer == null) continue;

                var ss = importer.defaultSampleSettings;
                if (ss.compressionFormat == cf && ss.loadType == lt) continue;

                ss.compressionFormat = cf;
                ss.loadType = lt;
                ss.quality = Mathf.Clamp01(quality);
                importer.defaultSampleSettings = ss;
                importer.SaveAndReimport();
                modified.Add(new { path = p });
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        result.SetResult(new
        {
            success = true,
            count = modified.Count,
            compressionFormat,
            loadType,
            modified
        });
    }
}
```
