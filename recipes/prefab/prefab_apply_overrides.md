# prefab_apply_overrides

Apply all overrides from a prefab instance to its source prefab asset. Equivalent to `prefab_apply`.

**Signature:** `PrefabApplyOverrides(string name = null, int instanceId = 0)`

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
  "appliedTo": "Assets/Prefabs/Enemy.prefab"
}
```

## Notes

- Functionally identical to `prefab_apply`; both call `PrefabUtility.ApplyPrefabInstance`.
- Applies overrides from the outermost prefab root to the source asset.
- All instances of the prefab in the scene will reflect the saved changes.
- Use `prefab_get_overrides` first to confirm what will be pushed to the asset.

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
        var (go, goErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId);
        if (goErr != null) { result.SetResult(goErr); return; }

        var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        if (prefabRoot == null) { result.SetResult(new { error = "Not a prefab instance" }); return; }

        WorkflowManager.SnapshotObject(prefabRoot);
        var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);
        PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);

        { result.SetResult(new { success = true, appliedTo = prefabPath }); return; }
    }
}
```
