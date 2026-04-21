# terrain_flatten

Flatten terrain to a specific height in a circular region with cosine falloff.

**Signature:** `TerrainFlatten(float normalizedX, float normalizedZ, float targetHeight = 0.5f, float radius = 0.1f, float strength = 1f, string name = null, int instanceId = 0)`

**Returns:** `{ success, centerX, centerZ, targetHeight, radius, strength }`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float normalizedX = 0.5f;
        float normalizedZ = 0.5f;
        float targetHeight = 0.5f;
        float radius = 0.1f;
        float strength = 1f;
        string name = null;
        int instanceId = 0;

        Terrain terrain = FindTerrain(name, instanceId);
        if (terrain == null)
        {
            result.SetResult(new { success = false, error = "Terrain not found" });
            return;
        }

        var data = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Flatten Terrain");

        int resolution = data.heightmapResolution;
        int centerX = Mathf.RoundToInt(normalizedX * (resolution - 1));
        int centerZ = Mathf.RoundToInt(normalizedZ * (resolution - 1));
        int radiusPixels = Mathf.RoundToInt(radius * resolution);

        int startX = Mathf.Max(0, centerX - radiusPixels);
        int startZ = Mathf.Max(0, centerZ - radiusPixels);
        int endX = Mathf.Min(resolution - 1, centerX + radiusPixels);
        int endZ = Mathf.Min(resolution - 1, centerZ + radiusPixels);

        int width = endX - startX + 1;
        int length = endZ - startZ + 1;

        float[,] heights = data.GetHeights(startX, startZ, width, length);

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int wx = startX + x;
                int wz = startZ + z;

                float dx = (wx - centerX) / (float)radiusPixels;
                float dz = (wz - centerZ) / (float)radiusPixels;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);

                if (distance <= 1f)
                {
                    float falloff = Mathf.Cos(distance * Mathf.PI * 0.5f);
                    heights[z, x] = Mathf.Lerp(heights[z, x], targetHeight, strength * falloff);
                }
            }
        }

        data.SetHeights(startX, startZ, heights);

        result.SetResult(new
        {
            success = true,
            centerX = normalizedX,
            centerZ = normalizedZ,
            targetHeight,
            radius,
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
