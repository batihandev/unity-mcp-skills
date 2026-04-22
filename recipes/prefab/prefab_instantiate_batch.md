# prefab_instantiate_batch

Instantiate multiple prefabs in one call via a typed item array.

**Signature:** `PrefabInstantiateBatch(PrefabInstantiateItem[] items)`

## Item fields

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `prefabPath` | string | Yes | - | Prefab asset path |
| `name` | string | No | prefab name | Instance name |
| `x`, `y`, `z` | float | No | 0 | Local position |
| `rotX`, `rotY`, `rotZ` | float | No | 0 | World euler angles |
| `scaleX`, `scaleY`, `scaleZ` | float | No | 1 | Local scale |
| `parentName`, `parentInstanceId`, `parentPath` | string/int/string | No | — | Parent resolution |

## Returns

`{ success, totalItems, successCount, failCount, results: [{ success, name, instanceId, position }] }`

## Notes

- Prefab assets are cached per path — repeated `prefabPath` values load the asset only once.
- If a prefab is not found by exact path, falls back to `AssetDatabase.FindAssets` by name.
- Each instance is registered with `Undo.RegisterCreatedObjectUndo`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _PrefabInstantiateItem
{
    public string prefabPath;
    public string name;
    public float x, y, z;
    public float rotX, rotY, rotZ;
    public float scaleX = 1, scaleY = 1, scaleZ = 1;
    public string parentName;
    public int parentInstanceId;
    public string parentPath;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _PrefabInstantiateItem { prefabPath = "Assets/Prefabs/Enemy.prefab", name = "Enemy_1" },
            new _PrefabInstantiateItem { prefabPath = "Assets/Prefabs/Enemy.prefab", name = "Enemy_2", x = 2 },
        };

        var prefabCache = new Dictionary<string, GameObject>();
        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.prefabPath))
            { results.Add(new { success = false, error = "prefabPath required" }); failCount++; continue; }

            if (!prefabCache.TryGetValue(item.prefabPath, out var prefab))
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.prefabPath);
                if (prefab == null)
                {
                    var guids = AssetDatabase.FindAssets(item.prefabPath + " t:Prefab");
                    if (guids.Length > 0)
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
                if (prefab != null) prefabCache[item.prefabPath] = prefab;
            }

            if (prefab == null)
            { results.Add(new { success = false, prefabPath = item.prefabPath, error = "prefab not found" }); failCount++; continue; }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            { results.Add(new { success = false, prefabPath = item.prefabPath, error = "instantiate failed" }); failCount++; continue; }

            if (!string.IsNullOrEmpty(item.parentName) || item.parentInstanceId != 0 || !string.IsNullOrEmpty(item.parentPath))
            {
                var (parentGo, parentErr) = GameObjectFinder.FindOrError(item.parentName, item.parentInstanceId, item.parentPath);
                if (parentErr != null)
                {
                    Object.DestroyImmediate(instance);
                    results.Add(new { success = false, error = "parent not found", prefabPath = item.prefabPath });
                    failCount++;
                    continue;
                }
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

            results.Add(new
            {
                success = true,
                name = instance.name,
                instanceId = instance.GetInstanceID(),
                position = new { x = item.x, y = item.y, z = item.z }
            });
            successCount++;
        }

        result.SetResult(new { success = true, totalItems = items.Length, successCount, failCount, results });
    }
}
```
