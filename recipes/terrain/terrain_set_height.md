# terrain_set_height

Set terrain height at a single normalized coordinate (0-1 range).

**Signature:** `TerrainSetHeight(float normalizedX, float normalizedZ, float height, string name = null, int instanceId = 0)`

**Returns:** `{ success, normalizedX, normalizedZ, height, pixelX, pixelZ }`

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
        float height = 0.5f;
        string name = null;
        int instanceId = 0;

        Terrain terrain = FindTerrain(name, instanceId);
        if (terrain == null)
        {
            result.SetResult(new { success = false, error = "Terrain not found" });
            return;
        }

        var data = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Set Terrain Height");

        int resolution = data.heightmapResolution;
        int x = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (resolution - 1)), 0, resolution - 1);
        int z = Mathf.Clamp(Mathf.RoundToInt(normalizedZ * (resolution - 1)), 0, resolution - 1);

        float[,] heights = data.GetHeights(x, z, 1, 1);
        heights[0, 0] = Mathf.Clamp01(height);
        data.SetHeights(x, z, heights);

        result.SetResult(new
        {
            success = true,
            normalizedX,
            normalizedZ,
            height = Mathf.Clamp01(height),
            pixelX = x,
            pixelZ = z
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
