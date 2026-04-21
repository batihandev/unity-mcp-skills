# terrain_generate_perlin

Generate natural-looking terrain using multi-octave Perlin noise.

**Signature:** `TerrainGeneratePerlin(float scale = 20f, float heightMultiplier = 0.3f, int octaves = 4, float persistence = 0.5f, float lacunarity = 2f, int seed = 0, string name = null, int instanceId = 0)`

**Returns:** `{ success, resolution, scale, heightMultiplier, octaves, persistence, lacunarity, seed }`

> **Parameters:** `scale` — lower values produce larger landscape features. `seed = 0` means random each time.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float scale = 20f;
        float heightMultiplier = 0.3f;
        int octaves = 4;
        float persistence = 0.5f;
        float lacunarity = 2f;
        int seed = 0;
        string name = null;
        int instanceId = 0;

        Terrain terrain = FindTerrain(name, instanceId);
        if (terrain == null)
        {
            result.SetResult(new { success = false, error = "Terrain not found" });
            return;
        }

        var data = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Generate Perlin Terrain");

        int resolution = data.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        System.Random random = seed != 0 ? new System.Random(seed) : new System.Random();
        float offsetX = random.Next(-10000, 10000);
        float offsetZ = random.Next(-10000, 10000);

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float noiseHeight = 0f;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x / (float)resolution * scale + offsetX) * frequency;
                    float sampleZ = (z / (float)resolution * scale + offsetZ) * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                heights[z, x] = Mathf.Clamp01(noiseHeight * heightMultiplier + 0.5f);
            }
        }

        data.SetHeights(0, 0, heights);

        result.SetResult(new
        {
            success = true,
            resolution,
            scale,
            heightMultiplier,
            octaves,
            persistence,
            lacunarity,
            seed = seed != 0 ? seed : (int?)null
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
