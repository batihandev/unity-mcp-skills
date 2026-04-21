# prefab_instantiate

Instantiate a single prefab asset into the current scene.

**Signature:** `PrefabInstantiate(string prefabPath, float x = 0, float y = 0, float z = 0, string name = null, string parentName = null, int parentInstanceId = 0, string parentPath = null)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `prefabPath` | string | Yes | - | Prefab asset path |
| `name` | string | No | prefab name | Name for the new instance |
| `x`, `y`, `z` | float | No | 0 | Local position (relative to parent if set) |
| `parentName` | string | No | null | Parent object name |
| `parentInstanceId` | int | No | 0 | Parent instance ID |
| `parentPath` | string | No | null | Parent hierarchy path |

## Returns

```json
{
  "success": true,
  "name": "Enemy(Clone)",
  "instanceId": 12345,
  "path": "Scene/Enemy(Clone)"
}
```

## Notes

- For spawning 2+ instances use `prefab_instantiate_batch` instead — one call is more efficient.
- Position is local to parent when `parentName`/`parentInstanceId`/`parentPath` is set, otherwise world-space.
- `prefab_spawn` does not exist — use this command.
- Operation is registered with `Undo.RegisterCreatedObjectUndo`.

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        /* Original Logic:

            GameObject parentGo = null;
            if (!string.IsNullOrEmpty(parentName) || parentInstanceId != 0 || !string.IsNullOrEmpty(parentPath))
            {
                var (found, parentErr) = GameObjectFinder.FindOrError(parentName, parentInstanceId, parentPath);
                if (parentErr != null) return parentErr;
                parentGo = found;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return new { error = $"Prefab not found: {prefabPath}" };

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                return new { error = $"Failed to instantiate prefab: {prefabPath}" };

            if (parentGo != null)
                instance.transform.SetParent(parentGo.transform, false);

            instance.transform.localPosition = new Vector3(x, y, z);

            if (!string.IsNullOrEmpty(name))
                instance.name = name;

            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");
            WorkflowManager.SnapshotObject(instance, SnapshotType.Created);

            return new { success = true, name = instance.name, instanceId = instance.GetInstanceID(), path = GameObjectFinder.GetPath(instance) };
        */
    }
}
```
