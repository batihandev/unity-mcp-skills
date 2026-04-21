# optimize_textures

Optimize texture settings (maxSize, compression) for all Texture2D assets matching an optional filter. Only affects textures of type `Default`; UI/Sprite textures are left untouched.

**Signature:** `OptimizeTextures(int maxTextureSize = 2048, bool enableCrunch = true, int compressionQuality = 50, string filter = "", int limit = 0)`

**Returns:** `{ success, count, message, modified }`

- `count` — number of textures actually changed
- `modified` — array of `{ path, name }` for each changed asset
- `limit = 0` means no cap (process all matches)

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int maxTextureSize = 2048;       // Textures larger than this will be capped
        bool enableCrunch = true;        // Enable crunch compression on Default textures
        int compressionQuality = 50;     // Crunch quality 0–100
        string filter = "";              // Extra AssetDatabase filter (e.g. "player")
        int limit = 0;                   // 0 = no limit; >0 = stop after N changes

        var guids = AssetDatabase.FindAssets("t:Texture2D " + filter);
        var modified = new List<object>();

        foreach (var guid in guids)
        {
            if (limit > 0 && modified.Count >= limit) break;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;

            if (importer.maxTextureSize > maxTextureSize)
            {
                importer.maxTextureSize = maxTextureSize;
                changed = true;
            }

            if (importer.textureType == TextureImporterType.Default)
            {
                if (importer.textureCompression != TextureImporterCompression.Compressed)
                {
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    changed = true;
                }

                if (enableCrunch && !importer.crunchedCompression)
                {
                    importer.crunchedCompression = true;
                    importer.compressionQuality = compressionQuality;
                    changed = true;
                }
            }

            if (changed)
            {
                importer.SaveAndReimport();
                modified.Add(new { path, name = System.IO.Path.GetFileName(path) });
            }
        }

        result.SetResult(new
        {
            success = true,
            count = modified.Count,
            message = $"Optimized {modified.Count} textures",
            modified
        });
    }
}
```
