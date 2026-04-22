# cleaner_fix_missing_scripts

Remove missing script components from all loaded GameObjects using `GameObjectUtility.RemoveMonoBehavioursWithMissingScript`. Each removal is registered with Undo.

**Signature:** `CleanerFixMissingScripts(bool includeInactive = true)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `includeInactive` | bool | No | true | Include inactive GameObjects |

## Returns

```json
{
  "success": true,
  "removedComponents": 5
}
```

## Notes

- Uses `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount` to detect affected objects, then `RemoveMonoBehavioursWithMissingScript` to clean them.
- Each removal is wrapped in `Undo.RegisterCompleteObjectUndo` so it can be reverted via Edit > Undo.
- When `includeInactive = true`, uses `Resources.FindObjectsOfTypeAll<GameObject>()` filtered to non-persistent, `HideFlags.None` objects — this includes inactive scene objects.
- Only operates on currently loaded scenes. Objects in unloaded scenes are not affected.
- To preview which objects have missing scripts before fixing, use `cleaner_find_missing_references` first.
- This operation is tracked by the workflow manager (`TracksWorkflow = true`).

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool includeInactive = true;

        var allObjects = includeInactive
            ? Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => !EditorUtility.IsPersistent(go) && go.hideFlags == HideFlags.None)
                .ToArray()
            : Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        int totalRemoved = 0;
        foreach (var go in allObjects)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (count > 0)
            {
                Undo.RegisterCompleteObjectUndo(go, "Fix Missing Scripts");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                totalRemoved += count;
            }
        }

        result.SetValue(new { success = true, removedComponents = totalRemoved });
    }
}
```
