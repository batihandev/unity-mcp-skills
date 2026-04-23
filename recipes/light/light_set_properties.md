# light_set_properties

Configure properties on an existing light. All property parameters are optional; only supplied values are changed.

**Signature:** `LightSetProperties(string name = null, int instanceId = 0, string path = null, float? r = null, float? g = null, float? b = null, float? intensity = null, float? range = null, float? spotAngle = null, string shadows = null)`

**Returns:** `{ success, name, lightType, color, intensity, range, spotAngle, shadows }`

**Notes:**
- Provide at least one of `name`, `instanceId`, or `path` to identify the target.
- `range` is only applied when the light is Point or Spot type.
- `spotAngle` is only applied for Spot lights.
- Invalid `shadows` value returns `{ warning }` instead of an error.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Point Light";
        int instanceId = 0;
        string path = null;
        float? r = 1f;
        float? g = 0.5f;
        float? b = 0f;
        float? intensity = 2f;
        float? range = 15f;
        float? spotAngle = null;
        string shadows = "soft";

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var light = go.GetComponent<Light>();
        if (light == null) { result.SetResult(new { error = $"No Light component on {go.name}" }); return; }

        WorkflowManager.SnapshotObject(light);
        Undo.RecordObject(light, "Set Light Properties");

        if (r.HasValue || g.HasValue || b.HasValue)
        {
            var c = light.color;
            light.color = new Color(r ?? c.r, g ?? c.g, b ?? c.b);
        }

        if (intensity.HasValue) light.intensity = intensity.Value;

        if (range.HasValue && (light.type == LightType.Point || light.type == LightType.Spot))
            light.range = range.Value;

        if (spotAngle.HasValue && light.type == LightType.Spot)
            light.spotAngle = spotAngle.Value;

        if (!string.IsNullOrEmpty(shadows))
        {
            switch (shadows.ToLower())
            {
                case "hard": light.shadows = LightShadows.Hard; break;
                case "soft": light.shadows = LightShadows.Soft; break;
                case "none": light.shadows = LightShadows.None; break;
                default:
                    result.SetResult(new { warning = $"Unknown shadow type: '{shadows}'. Valid values: hard, soft, none" });
                    return;
            }
        }

        result.SetResult(new
        {
            success = true,
            name = go.name,
            lightType = light.type.ToString(),
            color = new { r = light.color.r, g = light.color.g, b = light.color.b },
            intensity = light.intensity,
            range = light.range,
            spotAngle = light.spotAngle,
            shadows = light.shadows.ToString()
        });
    }
}
```
