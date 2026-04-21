# prefab_find_instances

Find all instances of a prefab asset currently present in the active scene.

**Signature:** `PrefabFindInstances(string prefabPath, int limit = 50)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `prefabPath` | string | Yes | - | Prefab asset path to search for |
| `limit` | int | No | 50 | Maximum number of instances to return |

## Returns

```json
{
  "success": true,
  "prefabPath": "Assets/Prefabs/Enemy.prefab",
  "count": 2,
  "instances": [
    { "name": "Enemy", "path": "Level/Enemy", "instanceId": 11111 },
    { "name": "Enemy (1)", "path": "Level/Enemy (1)", "instanceId": 22222 }
  ]
}
```

## Notes

- Read-only — does not modify the scene.
- Matches on exact `prefabPath` string; ensure the path matches the actual asset path.
- `limit` defaults to 50 to avoid large result payloads in busy scenes.
- Searches all GameObjects in the active scene regardless of hierarchy depth.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if (Validate.Required(prefabPath, "prefabPath") is object err) { result.SetResult(err); return; }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { result.SetResult(new { error = $"Prefab not found: {prefabPath}" }); return; }

        var allObjects = FindHelper.FindAll<GameObject>();
        var instances = allObjects
            .Where(go => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == prefabPath)
            .Take(limit)
            .Select(go => new { name = go.name, path = GameObjectFinder.GetPath(go), instanceId = go.GetInstanceID() })
            .ToArray();

        { result.SetResult(new { success = true, prefabPath, count = instances.Length, instances }); return; }
    }
}
```
