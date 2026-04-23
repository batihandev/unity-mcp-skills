# light_create

Create a new light GameObject in the scene.

**Signature:** `LightCreate(string name = "New Light", string lightType = "Point", float x = 0, float y = 3, float z = 0, float r = 1, float g = 1, float b = 1, float intensity = 1, float range = 10, float spotAngle = 30, string shadows = "soft")`

**Returns:** `{ success, name, instanceId, lightType, position, color, intensity, shadows }`

**Notes:**
- `lightType` accepts: `Directional`, `Point`, `Spot`, `Area` (case-insensitive).
- `range` applies to Point and Spot only; `spotAngle` applies to Spot only.
- `shadows` defaults to `"soft"`; invalid value falls back to `None`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Sun";
        string lightType = "Directional";
        float x = 0f, y = 3f, z = 0f;
        float r = 1f, g = 0.95f, b = 0.8f;
        float intensity = 1f;
        float range = 10f;
        float spotAngle = 30f;
        string shadows = "soft";

        var go = new GameObject(name);
        var light = go.AddComponent<Light>();

        if (System.Enum.TryParse<LightType>(lightType, true, out var lt))
            light.type = lt;
        else
        {
            Object.DestroyImmediate(go);
            result.SetResult(new { error = $"Unknown light type: {lightType}. Use: Directional, Point, Spot, Area" });
            return;
        }

        go.transform.position = new Vector3(x, y, z);
        light.color = new Color(r, g, b);
        light.intensity = intensity;

        if (lt == LightType.Point || lt == LightType.Spot)
            light.range = range;

        if (lt == LightType.Spot)
            light.spotAngle = spotAngle;

        switch (shadows.ToLower())
        {
            case "hard": light.shadows = LightShadows.Hard; break;
            case "soft": light.shadows = LightShadows.Soft; break;
            default:     light.shadows = LightShadows.None; break;
        }

        Undo.RegisterCreatedObjectUndo(go, "Create Light");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            lightType = light.type.ToString(),
            position = new { x, y, z },
            color = new { r, g, b },
            intensity,
            shadows = light.shadows.ToString()
        });
    }
}
```
