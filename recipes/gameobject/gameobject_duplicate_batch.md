# gameobject_duplicate_batch

Duplicate multiple GameObjects in one call.

**Signature:** `GameObjectDuplicateBatch(string items)`

`items`: JSON array of objects `{ name, instanceId, path }`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, originalName, copyName, copyInstanceId, copyPath }] }`

## Notes

- Each copy is placed under the same parent as its original and named `<originalName>_Copy`.
- A missing source object causes that item to fail without stopping the rest.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
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
        string items = @"[
            { ""name"": ""Cube"" },
            { ""instanceId"": 12345 },
            { ""path"": ""Parent/ChildObject"" }
        ]";

        { result.SetResult(BatchExecutor.Execute<BatchDuplicateItem>(items, item =>
        {
            var (go, error) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (error != null) throw new System.Exception("Object not found");

            var copy = Object.Instantiate(go, go.transform.parent);
            copy.name = go.name + "_Copy";
            Undo.RegisterCreatedObjectUndo(copy, "Batch Duplicate " + go.name);
            WorkflowManager.SnapshotObject(copy, SnapshotType.Created);

            return new
            {
                success = true,
                originalName = go.name,
                copyName = copy.name,
                copyInstanceId = copy.GetInstanceID(),
                copyPath = GameObjectFinder.GetPath(copy)
            };
        }, item => item.name ?? item.path ?? item.instanceId.ToString())); return; }
    }
}
```
