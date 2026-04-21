# light_set_properties_batch

Set properties on multiple lights in one call. Prefer over repeated `light_set_properties` when operating on 2+ lights.

**Signature:** `LightSetPropertiesBatch(string items)`

`items` is a JSON array where each element may contain: `name`, `instanceId`, `path` (identifier — provide at least one) and any of: `r`, `g`, `b`, `intensity`, `range`, `shadows`.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, name }] }`

**Notes:**
- `range` is silently skipped for Directional/Area lights (not applicable).
- Invalid `shadows` value throws and counts the item as a failure.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // items: JSON array of light property updates
        string items = @"[
            { ""name"": ""Point Light 1"", ""intensity"": 2.0, ""r"": 1.0, ""g"": 0.5, ""b"": 0.0, ""shadows"": ""soft"" },
            { ""name"": ""Point Light 2"", ""intensity"": 1.5, ""range"": 20.0 },
            { ""instanceId"": 12345,        ""r"": 0.8, ""g"": 0.8, ""b"": 1.0 }
        ]";

        result.SetResult(BatchExecutor.Execute<BatchLightPropsItem>(items, item =>
        {
            var (go, error) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (error != null) throw new System.Exception("Object not found");

            var light = go.GetComponent<Light>();
            if (light == null) throw new System.Exception("No Light component");

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
            if (!string.IsNullOrEmpty(item.shadows))
            {
                switch (item.shadows.ToLower())
                {
                    case "hard": light.shadows = LightShadows.Hard; break;
                    case "soft": light.shadows = LightShadows.Soft; break;
                    case "none": light.shadows = LightShadows.None; break;
                    default: throw new System.Exception($"Unknown shadow type: '{item.shadows}'. Valid values: hard, soft, none");
                }
            }

            return new { target = go.name, success = true };
        }, item => item.name ?? item.path ?? item.instanceId.ToString()));
    }
}

internal class BatchLightPropsItem
{
    public string name { get; set; }
    public int instanceId { get; set; }
    public string path { get; set; }
    public float? r { get; set; }
    public float? g { get; set; }
    public float? b { get; set; }
    public float? intensity { get; set; }
    public float? range { get; set; }
    public string shadows { get; set; }
}
```
