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
using System.Collections.Generic;

internal sealed class _BatchLightEnabledItem
{
    public string name;
    public int instanceId;
    public string path;
    public bool enabled;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchLightEnabledItem { name = "Point Light 1", enabled = false },
            new _BatchLightEnabledItem { name = "Point Light 2", enabled = false },
            new _BatchLightEnabledItem { instanceId = 12345, enabled = true },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var target = item.name ?? item.path ?? ("#" + item.instanceId);
            var (go, err) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (err != null) { results.Add(new { target, success = false, error = "Object not found" }); failCount++; continue; }

            var light = go.GetComponent<Light>();
            if (light == null) { results.Add(new { target, success = false, error = "No Light component" }); failCount++; continue; }

            WorkflowManager.SnapshotObject(light);
            Undo.RecordObject(light, "Batch Set Light Enabled");
            light.enabled = item.enabled;
            results.Add(new { target = go.name, success = true, enabled = item.enabled });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
