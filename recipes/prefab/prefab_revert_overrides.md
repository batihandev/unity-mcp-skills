# prefab_revert_overrides

Revert all overrides on a prefab instance back to the values defined in the source prefab asset.

**Signature:** `PrefabRevertOverrides(string name = null, int instanceId = 0)`

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
  "reverted": "Enemy"
}
```

## Notes

- Discards ALL overrides on the outermost prefab root — this includes property changes, added/removed components, and added GameObjects.
- Uses `PrefabUtility.RevertPrefabInstance` under the hood.
- The operation is recorded with `Undo.RecordObject` before reverting, so it can be undone.
- Use `prefab_get_overrides` first to inspect what will be discarded.

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
        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId);
        if (findErr != null) { result.SetResult(findErr); return; }

        var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        if (prefabRoot == null) { result.SetResult(new { error = "Not a prefab instance" }); return; }

        WorkflowManager.SnapshotObject(prefabRoot);
        Undo.RecordObject(prefabRoot, "Revert Prefab Overrides");
        PrefabUtility.RevertPrefabInstance(prefabRoot, InteractionMode.UserAction);

        { result.SetResult(new { success = true, reverted = prefabRoot.name }); return; }
    }
}
```
