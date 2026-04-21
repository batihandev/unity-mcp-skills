# validate_texture_sizes

Find Texture2D assets that exceed a recommended size threshold.

**Signature:** `ValidateTextureSizes(maxRecommendedSize int = 2048, limit int = 50)`

**Returns:** `{ maxRecommendedSize, largeTextureCount, textures: [{ path, name, width, height, maxTextureSize, format, recommendation }] }`

**Notes:**
- Only checks assets with a valid `TextureImporter` (i.e., imported Texture2D assets)
- `maxTextureSize` in results is the import setting cap, which may differ from actual texture dimensions

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int maxRecommendedSize = 2048;
        int limit = 50;

        var largeTextures = new List<object>();
        var guids = AssetDatabase.FindAssets("t:Texture2D");

        foreach (var guid in guids)
        {
            if (largeTextures.Count >= limit) break;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) continue;

            if (texture.width > maxRecommendedSize || texture.height > maxRecommendedSize)
            {
                largeTextures.Add(new
                {
                    path,
                    name = texture.name,
                    width = texture.width,
                    height = texture.height,
                    maxTextureSize = importer.maxTextureSize,
                    format = texture.format.ToString(),
                    recommendation = $"Consider reducing to {maxRecommendedSize}x{maxRecommendedSize} or smaller"
                });
            }
        }

        result.SetResult(new
        {
            maxRecommendedSize,
            largeTextureCount = largeTextures.Count,
            textures = largeTextures
        });
    }
}
```
