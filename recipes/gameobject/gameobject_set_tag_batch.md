# gameobject_set_tag_batch

Set the tag for multiple GameObjects in one call.

**Signature:** `GameObjectSetTagBatch(string items)`

`items`: JSON array of objects `{ name, instanceId, path, tag }`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target, tag }] }`

## Notes

- `tag` must match a tag defined in the project (e.g., `"Player"`, `"Enemy"`, `"Untagged"`).
- A missing object causes that item to fail without stopping the rest.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchSetTagItem
{
    public string name;
    public int instanceId;
    public string path;
    public string tag;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchSetTagItem { name = "Hero", tag = "Player" },
            new _BatchSetTagItem { name = "Goblin", tag = "Enemy" },
            new _BatchSetTagItem { instanceId = 12345, tag = "Untagged" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var target = item.name ?? item.path ?? ("#" + item.instanceId);
            var (go, err) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (err != null) { results.Add(new { target, success = false, error = "Object not found" }); failCount++; continue; }

            WorkflowManager.SnapshotObject(go);
            Undo.RecordObject(go, "Batch Set Tag");
            go.tag = item.tag;
            results.Add(new { target = go.name, success = true, tag = item.tag });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
