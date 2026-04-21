# gameobject_rename_batch

Rename multiple GameObjects in one call.

**Signature:** `GameObjectRenameBatch(string items)`

`items`: JSON array of objects `{ name, instanceId, path, newName }`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, oldName, newName, instanceId }] }`

## Notes

- Each item requires at least one identifier (`name`, `instanceId`, or `path`) and a `newName`.
- A missing object or missing `newName` causes that item to fail without stopping the rest.

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
            { ""name"": ""Cube"", ""newName"": ""Ground"" },
            { ""instanceId"": 12345, ""newName"": ""Player"" },
            { ""path"": ""Parent/OldChild"", ""newName"": ""NewChild"" }
        ]";

        { result.SetResult(BatchExecutor.Execute<BatchRenameItem>(items, item =>
        {
            if (string.IsNullOrEmpty(item.newName))
                throw new System.Exception("newName is required");

            var (go, error) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (error != null) throw new System.Exception("Object not found");

            var oldName = go.name;
            WorkflowManager.SnapshotObject(go);
            Undo.RecordObject(go, "Batch Rename " + go.name);
            go.name = item.newName;

            return new { success = true, oldName, newName = go.name, instanceId = go.GetInstanceID() };
        }, item => item.name ?? item.path ?? item.instanceId.ToString())); return; }
    }
}
```
