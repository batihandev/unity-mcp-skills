# prefab_apply

Apply all changes from a prefab instance back to its source prefab asset. Equivalent to `prefab_apply_overrides`.

**Signature:** `PrefabApply(string name = null, int instanceId = 0, string path = null)`

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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;

        var (go, goErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (goErr != null) { result.SetResult(goErr); return; }

        var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        if (prefabRoot == null)
            { result.SetResult(new { error = "GameObject is not a prefab instance" }); return; }

        WorkflowManager.SnapshotObject(prefabRoot);
        var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);
        PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);

        { result.SetResult(new { success = true, appliedTo = prefabPath }); return; }
    }
}
```
