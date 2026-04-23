# prefab_find_instances

Find all instances of a prefab asset currently present in the active scene.

**Signature:** `PrefabFindInstances(string prefabPath, int limit = 50)`

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
- Matches on exact `prefabPath` string; ensure the path matches the actual asset path.
- `limit` defaults to 50 to avoid large result payloads in busy scenes.
- Searches all GameObjects in the active scene regardless of hierarchy depth.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string prefabPath = null;
        int limit = 50;

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
