# terrain_smooth

Smooth terrain heights in a region by averaging each pixel with its 8 neighbors.

**Signature:** `TerrainSmooth(float normalizedX, float normalizedZ, float radius = 0.1f, int iterations = 1, string name = null, int instanceId = 0)`

**Returns:** `{ success, centerX, centerZ, radius, iterations, affectedArea }`

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
        float normalizedX = 0.5f;
        float normalizedZ = 0.5f;
        float radius = 0.1f;
        int iterations = 1;
        string name = null;
        int instanceId = 0;

        Terrain terrain = FindTerrain(name, instanceId);
        if (terrain == null)
        {
            result.SetResult(new { success = false, error = "Terrain not found" });
            return;
        }

        var data = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Smooth Terrain");

        int resolution = data.heightmapResolution;
        int centerX = Mathf.RoundToInt(normalizedX * (resolution - 1));
        int centerZ = Mathf.RoundToInt(normalizedZ * (resolution - 1));
        int radiusPixels = Mathf.RoundToInt(radius * resolution);

        int startX = Mathf.Max(1, centerX - radiusPixels);
        int startZ = Mathf.Max(1, centerZ - radiusPixels);
        int endX = Mathf.Min(resolution - 2, centerX + radiusPixels);
        int endZ = Mathf.Min(resolution - 2, centerZ + radiusPixels);

        int width = endX - startX + 1;
        int length = endZ - startZ + 1;

        for (int iter = 0; iter < iterations; iter++)
        {
            float[,] heights = data.GetHeights(startX - 1, startZ - 1, width + 2, length + 2);
            float[,] smoothed = new float[length, width];

            for (int z = 0; z < length; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    for (int dz = 0; dz <= 2; dz++)
                        for (int dx = 0; dx <= 2; dx++)
                            sum += heights[z + dz, x + dx];
                    smoothed[z, x] = sum / 9f;
                }
            }

            data.SetHeights(startX, startZ, smoothed);
        }

        result.SetResult(new
        {
            success = true,
            centerX = normalizedX,
            centerZ = normalizedZ,
            radius,
            iterations,
            affectedArea = new { startX, startZ, width, length }
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
        return Object.FindFirstObjectByType<Terrain>();
    }
}
```
