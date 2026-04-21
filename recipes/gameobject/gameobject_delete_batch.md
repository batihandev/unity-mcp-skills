# gameobject_delete_batch

Delete multiple GameObjects in one call.

**Signature:** `GameObjectDeleteBatch(string items)`

`items`: JSON array of **strings** (object names) or **objects** `{ name, instanceId, path }`. Both forms can be mixed in the same array.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target }] }`

## Notes

- Accepts a plain string array (`["Cube", "Sphere"]`) for convenience, in addition to the full object form.
- Each item is normalized internally; a missing object causes that item to fail without stopping the rest.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Simple form: array of names
        string items = @"[""Cube"", ""Sphere"", ""EmptyObj""]";

        // Full form: array of objects (can also use instanceId or path)
        // string items = @"[
        //     { ""name"": ""Cube"" },
        //     { ""instanceId"": 12345 },
        //     { ""path"": ""Parent/Child"" }
        // ]";

        if (Validate.RequiredJsonArray(items, "items") is object err) { result.SetResult(err); return; }

        try
        {
            var normalizedItems = NormalizeDeleteBatchItems(items);
            { result.SetResult(BatchExecutor.Execute<BatchDeleteItem>(normalizedItems, item =>
            {
                var (go, error) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
                if (error != null)
                    throw new System.Exception("Object not found");

                var deletedName = go.name;
                WorkflowManager.SnapshotObject(go);
                Undo.DestroyObjectImmediate(go);
                return new { target = deletedName, success = true };
            }, item => item.name ?? item.path ?? item.instanceId.ToString())); return; }
        }
        catch (System.Exception ex)
        {
            { result.SetResult(new { error = $"Failed to parse items JSON: {ex.Message}" }); return; }
        }
    }
}
```
