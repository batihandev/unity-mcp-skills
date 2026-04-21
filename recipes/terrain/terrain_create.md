# terrain_create

Create a new Terrain GameObject with a TerrainData asset.

**Signature:** `TerrainCreate(string name = "Terrain", int width = 500, int length = 500, int height = 100, int heightmapResolution = 513, float x = 0, float y = 0, float z = 0)`

**Returns:** `{ success, name, instanceId, terrainDataPath, size, position }`

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
        string name = "Terrain";
        int width = 500;
        int length = 500;
        int height = 100;
        int heightmapResolution = 513;
        float x = 0f;
        float y = 0f;
        float z = 0f;

        var terrainData = new TerrainData();
        terrainData.heightmapResolution = heightmapResolution;
        terrainData.size = new Vector3(width, height, length);

        var assetPath = $"Assets/{name}_Data.asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
        AssetDatabase.CreateAsset(terrainData, assetPath);

        var terrainGO = Terrain.CreateTerrainGameObject(terrainData);
        terrainGO.name = name;
        terrainGO.transform.position = new Vector3(x, y, z);

        Undo.RegisterCreatedObjectUndo(terrainGO, "Create Terrain");
        AssetDatabase.SaveAssets();

        result.SetResult(new
        {
            success = true,
            name = terrainGO.name,
            instanceId = terrainGO.GetInstanceID(),
            terrainDataPath = assetPath,
            size = new { width, length, height },
            position = new { x, y, z }
        });
    }
}
```
