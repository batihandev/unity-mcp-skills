# prefab_apply

Apply all changes from a prefab instance back to its source prefab asset. Equivalent to `prefab_apply_overrides`.

**Signature:** `PrefabApply(string name = null, int instanceId = 0, string path = null)`

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | Prefab instance name |
| `instanceId` | int | No* | Instance ID (preferred) |
| `path` | string | No* | Hierarchy path |

*At least one identifier required.

## Returns

```json
{
  "success": true,
  "appliedTo": "Assets/Prefabs/Enemy.prefab"
}
```

## Notes

- The target must be a prefab instance in the scene; returns an error if the object is not connected to a prefab.
- Uses the outermost prefab root — even if you pass a child object the operation applies to the root.
- `prefab_save` does not exist — use this command.
- Equivalent to `prefab_apply_overrides`; both call `PrefabUtility.ApplyPrefabInstance`.

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        /* Original Logic:

            var (go, goErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
            if (goErr != null) return goErr;

            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            if (prefabRoot == null)
                return new { error = "GameObject is not a prefab instance" };

            WorkflowManager.SnapshotObject(prefabRoot);
            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);
            PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);

            return new { success = true, appliedTo = prefabPath };
        */
    }
}
```
