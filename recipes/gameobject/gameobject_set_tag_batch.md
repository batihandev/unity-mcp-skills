# gameobject_set_tag_batch

Set the tag for multiple GameObjects in one call.

**Signature:** `GameObjectSetTagBatch(string items)`

`items`: JSON array of objects `{ name, instanceId, path, tag }`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target, tag }] }`

## Notes

- `tag` must match a tag defined in the project (e.g., `"Player"`, `"Enemy"`, `"Untagged"`).
- A missing object causes that item to fail without stopping the rest.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string items = @"[
            { ""name"": ""Hero"", ""tag"": ""Player"" },
            { ""name"": ""Goblin"", ""tag"": ""Enemy"" },
            { ""instanceId"": 12345, ""tag"": ""Untagged"" }
        ]";

        /* Original Logic:

            return BatchExecutor.Execute<BatchSetTagItem>(items, item =>
            {
                var (go, error) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
                if (error != null) throw new System.Exception("Object not found");

                WorkflowManager.SnapshotObject(go);
                Undo.RecordObject(go, "Batch Set Tag");
                go.tag = item.tag;
                return new { target = go.name, success = true, tag = item.tag };
            }, item => item.name ?? item.path);
        */
    }
}
```
