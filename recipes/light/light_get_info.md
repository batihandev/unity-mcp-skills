# light_get_info

Get detailed information about a single light component.

**Signature:** `LightGetInfo(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ name, instanceId, path, lightType, color, intensity, range, spotAngle, shadows, enabled, cullingMask, bounceIntensity }`

**Notes:**
- Read-only; no undo entry is created.
- Returns `cullingMask` and `bounceIntensity` in addition to the basic properties.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Directional Light";
        int instanceId = 0;
        string path = null;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var light = go.GetComponent<Light>();
        if (light == null) { result.SetResult(new { error = $"No Light component on {go.name}" }); return; }

        result.SetResult(new
        {
            name = go.name,
            instanceId = go.GetInstanceID(),
            path = GameObjectFinder.GetPath(go),
            lightType = light.type.ToString(),
            color = new { r = light.color.r, g = light.color.g, b = light.color.b },
            intensity = light.intensity,
            range = light.range,
            spotAngle = light.spotAngle,
            shadows = light.shadows.ToString(),
            enabled = light.enabled,
            cullingMask = light.cullingMask,
            bounceIntensity = light.bounceIntensity
        });
    }
}
```
