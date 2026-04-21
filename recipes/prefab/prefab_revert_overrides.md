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

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        /* Original Logic:

            var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId);
            if (findErr != null) return findErr;

            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            if (prefabRoot == null) return new { error = "Not a prefab instance" };

            WorkflowManager.SnapshotObject(prefabRoot);
            Undo.RecordObject(prefabRoot, "Revert Prefab Overrides");
            PrefabUtility.RevertPrefabInstance(prefabRoot, InteractionMode.UserAction);

            return new { success = true, reverted = prefabRoot.name };
        */
    }
}
```
