# gameobject_set_active_batch

Enable or disable multiple GameObjects in one call.

**Signature:** `GameObjectSetActiveBatch(string items)`

`items`: JSON array of objects `{ name, instanceId, path, active }`. `active` defaults to `true`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, target, active }] }`

## Notes

- `active` defaults to `true` per item if omitted.
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
            { ""name"": ""Enemy1"", ""active"": false },
            { ""name"": ""Enemy2"", ""active"": false },
            { ""instanceId"": 12345, ""active"": true }
        ]";

        /* Original Logic:

            return BatchExecutor.Execute<BatchSetActiveItem>(items, item =>
            {
                var (go, error) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
                if (error != null) throw new System.Exception("Object not found");

                WorkflowManager.SnapshotObject(go);
                Undo.RecordObject(go, "Batch Set Active");
                go.SetActive(item.active);
                return new { target = go.name, success = true, active = item.active };
            }, item => item.name ?? item.path);
        */
    }
}
```
