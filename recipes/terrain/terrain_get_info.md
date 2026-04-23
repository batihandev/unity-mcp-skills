# terrain_get_info

Get terrain information including size, resolution, and texture layers.

**Signature:** `TerrainGetInfo(string name = null, int instanceId = 0)`

**Returns:** `{ success, name, instanceId, position, size, heightmapResolution, alphamapResolution, detailResolution, terrainLayerCount, layers }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;

        Terrain terrain = FindTerrain(name, instanceId);
        if (terrain == null)
        {
            result.SetResult(new { success = false, error = "Terrain not found" });
            return;
        }

        var data = terrain.terrainData;
        var layers = new List<object>();

        if (data.terrainLayers != null)
        {
            foreach (var layer in data.terrainLayers)
            {
                if (layer != null)
                {
                    layers.Add(new
                    {
                        name = layer.name,
                        diffuseTexture = layer.diffuseTexture?.name,
                        tileSize = new { x = layer.tileSize.x, y = layer.tileSize.y }
                    });
                }
            }
        }

        result.SetResult(new
        {
            success = true,
            name = terrain.name,
            instanceId = terrain.gameObject.GetInstanceID(),
            position = new { x = terrain.transform.position.x, y = terrain.transform.position.y, z = terrain.transform.position.z },
            size = new { width = data.size.x, height = data.size.y, length = data.size.z },
            heightmapResolution = data.heightmapResolution,
            alphamapResolution = data.alphamapResolution,
            detailResolution = data.detailResolution,
            terrainLayerCount = data.terrainLayers?.Length ?? 0,
            layers
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
