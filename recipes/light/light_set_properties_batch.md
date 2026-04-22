# light_set_properties_batch

Set properties on multiple lights in one call. Prefer over repeated `light_set_properties` when operating on 2+ lights.

**Signature:** `LightSetPropertiesBatch(string items)`

`items` is a JSON array where each element may contain: `name`, `instanceId`, `path` (identifier — provide at least one) and any of: `r`, `g`, `b`, `intensity`, `range`, `shadows`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, name }] }`

**Notes:**
- `range` is silently skipped for Directional/Area lights (not applicable).
- Invalid `shadows` value throws and counts the item as a failure.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal sealed class _BatchLightPropsItem
{
    public string name;
    public int instanceId;
    public string path;
    public float? r, g, b;
    public float? intensity;
    public float? range;
    public string shadows;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchLightPropsItem { name = "Point Light 1", intensity = 2.0f, r = 1.0f, g = 0.5f, b = 0.0f, shadows = "soft" },
            new _BatchLightPropsItem { name = "Point Light 2", intensity = 1.5f, range = 20.0f },
            new _BatchLightPropsItem { instanceId = 12345, r = 0.8f, g = 0.8f, b = 1.0f },
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
            Undo.RecordObject(light, "Batch Set Light Properties");

            if (item.r.HasValue || item.g.HasValue || item.b.HasValue)
            {
                var c = light.color;
                light.color = new Color(item.r ?? c.r, item.g ?? c.g, item.b ?? c.b);
            }
            if (item.intensity.HasValue) light.intensity = item.intensity.Value;
            if (item.range.HasValue && (light.type == LightType.Point || light.type == LightType.Spot))
                light.range = item.range.Value;

            bool ok = true;
            if (!string.IsNullOrEmpty(item.shadows))
            {
                switch (item.shadows.ToLower())
                {
                    case "hard": light.shadows = LightShadows.Hard; break;
                    case "soft": light.shadows = LightShadows.Soft; break;
                    case "none": light.shadows = LightShadows.None; break;
                    default:
                        results.Add(new { target, success = false, error = "Unknown shadow type: " + item.shadows });
                        failCount++;
                        ok = false;
                        break;
                }
            }
            if (!ok) continue;

            results.Add(new { target = go.name, success = true });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
