# terrain_get_height

Get terrain height at a world-space XZ position.

**Signature:** `TerrainGetHeight(float worldX, float worldZ, string name = null, int instanceId = 0)`

**Returns:** `{ success, worldX, worldZ, height, worldY }`

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
        float worldX = 0f;
        float worldZ = 0f;
        string name = null;
        int instanceId = 0;

        Terrain terrain = FindTerrain(name, instanceId);
        if (terrain == null)
        {
            result.SetResult(new { success = false, error = "Terrain not found" });
            return;
        }

        float height = terrain.SampleHeight(new Vector3(worldX, 0, worldZ));

        result.SetResult(new
        {
            success = true,
            worldX,
            worldZ,
            height,
            worldY = height + terrain.transform.position.y
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
