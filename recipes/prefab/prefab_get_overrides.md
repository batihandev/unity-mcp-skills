# prefab_get_overrides

Inspect all property overrides, added components, removed components, and added GameObjects on a prefab instance.

**Signature:** `PrefabGetOverrides(string name = null, int instanceId = 0)`

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Prefab instance name |
| `instanceId` | int | No* | Instance ID (preferred) |

*At least one identifier required.

## Returns

```json
{
  "success": true,
  "prefabPath": "Assets/Prefabs/Enemy.prefab",
  "propertyOverrides": 3,
  "addedComponents": 1,
  "removedComponents": 0,
  "addedGameObjects": 0,
  "hasOverrides": true
}
```

## Notes

- Read-only — does not modify anything.
- Uses the outermost prefab root even when a child is passed.
- `propertyOverrides` is the count of `PropertyModification` entries where the target is non-null (internal Unity bookkeeping entries with null targets are excluded).
- Check `hasOverrides` for a quick true/false before deciding whether to apply or revert.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var (go, goErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId);
        if (goErr != null) { result.SetResult(goErr); return; }

        var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        if (prefabRoot == null) { result.SetResult(new { error = "Not a prefab instance" }); return; }

        var overrides = PrefabUtility.GetPropertyModifications(prefabRoot);
        var addedComponents = PrefabUtility.GetAddedComponents(prefabRoot);
        var removedComponents = PrefabUtility.GetRemovedComponents(prefabRoot);
        var addedObjects = PrefabUtility.GetAddedGameObjects(prefabRoot);

        var propOverrides = new System.Collections.Generic.List<object>();
        if (overrides != null)
        {
            foreach (var o in overrides)
            {
                if (o.target == null) continue;
                propOverrides.Add(new { 
                    target = o.target.name, 
                    property = o.propertyPath, 
                    value = o.value 
                });
            }
        }

        { result.SetResult(new
        {
            success = true,
            prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot),
            propertyOverrides = propOverrides.Count,
            addedComponents = addedComponents.Count,
            removedComponents = removedComponents.Count,
            addedGameObjects = addedObjects.Count,
            hasOverrides = propOverrides.Count > 0 || addedComponents.Count > 0 || removedComponents.Count > 0
        }); return; }
    }
}
```
