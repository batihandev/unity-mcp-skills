# gameobject_set_active_batch

Enable or disable multiple GameObjects in one call via a typed item array.

**Signature:** `GameObjectSetActiveBatch(_BatchSetActiveItem[] items)`

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target, active }] }`

## Notes

- `active` defaults to `true` per item if omitted.
- A missing object causes that item to fail without stopping the rest.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder.FindOrError`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.SnapshotObject`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchSetActiveItem
{
    public string name;
    public int instanceId;
    public string path;
    public bool active = true;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchSetActiveItem { name = "Enemy1", active = false },
            new _BatchSetActiveItem { name = "Enemy2", active = false },
            new _BatchSetActiveItem { instanceId = 12345, active = true },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var (go, err) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (err != null) { results.Add(new { success = false, target = item.name ?? item.path, error = err }); failCount++; continue; }

            WorkflowManager.SnapshotObject(go);
            Undo.RecordObject(go, "Batch Set Active");
            go.SetActive(item.active);

            results.Add(new { success = true, target = go.name, active = item.active });
            successCount++;
        }

        result.SetResult(new { success = true, totalItems = items.Length, successCount, failCount, results });
    }
}
```
