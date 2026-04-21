# light_set_enabled_batch

Enable or disable multiple lights in one call. Prefer over repeated `light_set_enabled` when operating on 2+ lights.

**Signature:** `LightSetEnabledBatch(string items)`

`items` is a JSON array where each element must contain an identifier (`name`, `instanceId`, or `path`) and `enabled` (bool).

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, name, enabled }] }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // items: JSON array of enable/disable operations
        string items = @"[
            { ""name"": ""Point Light 1"", ""enabled"": false },
            { ""name"": ""Point Light 2"", ""enabled"": false },
            { ""instanceId"": 12345,        ""enabled"": true }
        ]";

        result.SetResult(BatchExecutor.Execute<BatchLightEnabledItem>(items, item =>
        {
            var (go, error) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (error != null) throw new System.Exception("Object not found");

            var light = go.GetComponent<Light>();
            if (light == null) throw new System.Exception("No Light component");

            WorkflowManager.SnapshotObject(light);
            Undo.RecordObject(light, "Batch Set Light Enabled");
            light.enabled = item.enabled;
            return new { target = go.name, success = true, enabled = item.enabled };
        }, item => item.name ?? item.path ?? item.instanceId.ToString()));
    }
}

internal class BatchLightEnabledItem
{
    public string name { get; set; }
    public int instanceId { get; set; }
    public string path { get; set; }
    public bool enabled { get; set; }
}
```
