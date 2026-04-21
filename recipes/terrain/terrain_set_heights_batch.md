# terrain_set_heights_batch

Set terrain heights in a rectangular region using a 2D array. Heights array is indexed `[z][x]` with values 0-1.

**Signature:** `TerrainSetHeightsBatch(int startX, int startZ, float[][] heights, string name = null, int instanceId = 0)`

**Returns:** `{ success, startX, startZ, modifiedWidth, modifiedLength, totalPointsModified }`

> **Note:** `heights` is a jagged array where the outer index is Z and the inner index is X. All values must be in the 0-1 range (normalized, not world units).

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int startX = 0;
        int startZ = 0;
        // Example: 3x3 patch of heights
        float[][] heights = new float[][]
        {
            new float[] { 0.1f, 0.2f, 0.1f },
            new float[] { 0.2f, 0.5f, 0.2f },
            new float[] { 0.1f, 0.2f, 0.1f }
        };
        string name = null;
        int instanceId = 0;

        Terrain terrain = FindTerrain(name, instanceId);
        if (terrain == null)
        {
            result.SetResult(new { success = false, error = "Terrain not found" });
            return;
        }

        if (heights == null || heights.Length == 0)
        {
            result.SetResult(new { success = false, error = "Heights array is empty" });
            return;
        }

        var data = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Set Terrain Heights Batch");

        int zSize = heights.Length;
        int xSize = heights[0].Length;
        int resolution = data.heightmapResolution;

        startX = Mathf.Clamp(startX, 0, resolution - 1);
        startZ = Mathf.Clamp(startZ, 0, resolution - 1);
        xSize = Mathf.Min(xSize, resolution - startX);
        zSize = Mathf.Min(zSize, resolution - startZ);

        float[,] heightData = new float[zSize, xSize];
        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                if (x < heights[z].Length)
                    heightData[z, x] = Mathf.Clamp01(heights[z][x]);
            }
        }

        data.SetHeights(startX, startZ, heightData);

        result.SetResult(new
        {
            success = true,
            startX,
            startZ,
            modifiedWidth = xSize,
            modifiedLength = zSize,
            totalPointsModified = xSize * zSize
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
