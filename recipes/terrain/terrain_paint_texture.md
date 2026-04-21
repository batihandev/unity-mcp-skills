# terrain_paint_texture

Paint a terrain texture layer at a normalized position. The terrain must have layers already configured.

**Signature:** `TerrainPaintTexture(float normalizedX, float normalizedZ, int layerIndex, float strength = 1f, int brushSize = 10, string name = null, int instanceId = 0)`

**Returns:** `{ success, layerIndex, layerName, centerX, centerZ, brushSize, strength }`

> **Prerequisite:** Call `terrain_get_info` first to verify available layer indices. Layer index is 0-based.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float normalizedX = 0.5f;
        float normalizedZ = 0.5f;
        int layerIndex = 0;
        float strength = 1f;
        int brushSize = 10;
        string name = null;
        int instanceId = 0;

        Terrain terrain = FindTerrain(name, instanceId);
        if (terrain == null)
        {
            result.SetResult(new { success = false, error = "Terrain not found" });
            return;
        }

        var data = terrain.terrainData;

        if (data.terrainLayers == null || layerIndex >= data.terrainLayers.Length)
        {
            result.SetResult(new { success = false, error = $"Layer index {layerIndex} out of range. Terrain has {data.terrainLayers?.Length ?? 0} layers." });
            return;
        }

        Undo.RegisterCompleteObjectUndo(data, "Paint Terrain Texture");

        int alphamapRes = data.alphamapResolution;
        int centerX = Mathf.RoundToInt(normalizedX * (alphamapRes - 1));
        int centerZ = Mathf.RoundToInt(normalizedZ * (alphamapRes - 1));

        int halfBrush = brushSize / 2;
        int startX = Mathf.Clamp(centerX - halfBrush, 0, alphamapRes - 1);
        int startZ = Mathf.Clamp(centerZ - halfBrush, 0, alphamapRes - 1);
        int endX = Mathf.Clamp(centerX + halfBrush, 0, alphamapRes - 1);
        int endZ = Mathf.Clamp(centerZ + halfBrush, 0, alphamapRes - 1);

        int width = endX - startX + 1;
        int height = endZ - startZ + 1;

        float[,,] alphamaps = data.GetAlphamaps(startX, startZ, width, height);
        int layerCount = alphamaps.GetLength(2);

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, z), new Vector2(width / 2f, height / 2f));
                float falloff = Mathf.Clamp01(1f - dist / halfBrush);
                float paintStrength = strength * falloff;

                for (int l = 0; l < layerCount; l++)
                {
                    alphamaps[z, x, l] = l == layerIndex
                        ? Mathf.Lerp(alphamaps[z, x, l], 1f, paintStrength)
                        : Mathf.Lerp(alphamaps[z, x, l], 0f, paintStrength);
                }

                // Normalize
                float sum = 0;
                for (int l = 0; l < layerCount; l++) sum += alphamaps[z, x, l];
                if (sum > 0)
                    for (int l = 0; l < layerCount; l++) alphamaps[z, x, l] /= sum;
            }
        }

        data.SetAlphamaps(startX, startZ, alphamaps);

        result.SetResult(new
        {
            success = true,
            layerIndex,
            layerName = data.terrainLayers[layerIndex]?.name,
            centerX,
            centerZ,
            brushSize,
            strength
        });
    }

    private Terrain FindTerrain(string name, int instanceId)
    {
        if (instanceId != 0)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            return obj?.GetComponent<Terrain>();
        }
        if (!string.IsNullOrEmpty(name))
        {
            var go = GameObject.Find(name);
            return go?.GetComponent<Terrain>();
        }
        return Object.FindObjectOfType<Terrain>();
    }
}
```
