# prefab_instantiate_batch

Instantiate multiple prefabs in one call. Use this whenever spawning 2 or more instances.

**Signature:** `PrefabInstantiateBatch(string items)`

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `items` | string (JSON array) | Yes | Array of instantiation configs (see item schema below) |

### Item Schema

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `prefabPath` | string | Yes | - | Prefab asset path |
| `name` | string | No | prefab name | Instance name |
| `x`, `y`, `z` | float | No | 0 | Local position |
| `rotX`, `rotY`, `rotZ` | float | No | 0 | World euler angles |
| `scaleX`, `scaleY`, `scaleZ` | float | No | 1 | Local scale |
| `parentName` | string | No | null | Parent object name |
| `parentInstanceId` | int | No | 0 | Parent instance ID |
| `parentPath` | string | No | null | Parent hierarchy path |

## Returns

```json
{
  "success": true,
  "totalItems": 3,
  "successCount": 3,
  "failCount": 0,
  "results": [
    { "success": true, "name": "Enemy_1", "instanceId": 11111, "position": { "x": 0, "y": 0, "z": 0 } }
  ]
}
```

## Notes

- Prefab assets are cached per path — repeated `prefabPath` values load the asset only once.
- If a prefab is not found by exact path, falls back to `AssetDatabase.FindAssets` by name.
- Each instance is registered with `Undo.RegisterCreatedObjectUndo`.
- Rotation is applied as world `eulerAngles`; scale as `localScale`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Cache loaded prefabs to avoid repeated AssetDatabase calls
        var prefabCache = new System.Collections.Generic.Dictionary<string, GameObject>();

        { result.SetResult(BatchExecutor.Execute<BatchInstantiateItem>(items, item =>
        {
            if (string.IsNullOrEmpty(item.prefabPath))
                throw new System.Exception("prefabPath required");

            if (!prefabCache.TryGetValue(item.prefabPath, out var prefab))
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.prefabPath);
                if (prefab == null)
                {
                    var guids = AssetDatabase.FindAssets(item.prefabPath + " t:Prefab");
                    if (guids.Length > 0)
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }

                if (prefab != null)
                    prefabCache[item.prefabPath] = prefab;
            }

            if (prefab == null)
                throw new System.Exception($"Prefab not found: {item.prefabPath}");

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                throw new System.Exception($"Failed to instantiate prefab: {item.prefabPath}");
            // Set parent if specified
            if (!string.IsNullOrEmpty(item.parentName) || item.parentInstanceId != 0 || !string.IsNullOrEmpty(item.parentPath))
            {
                var (parentGo, parentErr) = GameObjectFinder.FindOrError(item.parentName, item.parentInstanceId, item.parentPath);
                if (parentErr != null) throw new System.Exception($"Parent not found for '{item.name ?? item.prefabPath}'");
                instance.transform.SetParent(parentGo.transform, false);
            }

            instance.transform.localPosition = new Vector3(item.x, item.y, item.z);

            if (item.rotX != 0 || item.rotY != 0 || item.rotZ != 0)
                instance.transform.eulerAngles = new Vector3(item.rotX, item.rotY, item.rotZ);

            if (item.scaleX != 1 || item.scaleY != 1 || item.scaleZ != 1)
                instance.transform.localScale = new Vector3(item.scaleX, item.scaleY, item.scaleZ);

            if (!string.IsNullOrEmpty(item.name))
                instance.name = item.name;

            Undo.RegisterCreatedObjectUndo(instance, "Batch Instantiate Prefab");
            WorkflowManager.SnapshotObject(instance, SnapshotType.Created);
            return new
            {
                success = true,
                name = instance.name,
                instanceId = instance.GetInstanceID(),
                position = new { x = item.x, y = item.y, z = item.z }
            };
        }, item => item.prefabPath)); return; }
    }
}
```
