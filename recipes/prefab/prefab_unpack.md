# prefab_unpack

Unpack a prefab instance, breaking its connection to the prefab asset.

**Signature:** `PrefabUnpack(string name = null, int instanceId = 0, string path = null, bool completely = false)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | No* | - | Prefab instance name |
| `instanceId` | int | No* | - | Instance ID (preferred) |
| `path` | string | No* | - | Hierarchy path |
| `completely` | bool | No | false | `true` = unpack all nested prefabs recursively; `false` = unpack outermost root only |

*At least one identifier required.

## Returns

```json
{
  "success": true,
  "unpacked": "Enemy"
}
```

## Notes

- `completely=false` (default) unpacks only the outermost prefab root (`PrefabUnpackMode.OutermostRoot`).
- `completely=true` recursively unpacks all nested prefabs (`PrefabUnpackMode.Completely`).
- After unpacking, the GameObject is a plain scene object with no prefab connection.
- Operation is undoable — the snapshot is recorded before unpacking.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        WorkflowManager.SnapshotObject(go);
        var mode = completely ? PrefabUnpackMode.Completely : PrefabUnpackMode.OutermostRoot;
        PrefabUtility.UnpackPrefabInstance(go, mode, InteractionMode.UserAction);

        { result.SetResult(new { success = true, unpacked = go.name }); return; }
    }
}
```
