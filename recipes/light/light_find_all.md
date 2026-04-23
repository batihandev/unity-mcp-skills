# light_find_all

Find all lights in the current scene, with optional type filtering.

**Signature:** `LightFindAll(string lightType = null, int limit = 50)`

**Returns:** `{ count, lights: [{ name, instanceId, path, lightType, intensity, enabled }] }`

**Notes:**
- Read-only; no undo entry is created.
- `lightType` filter is case-insensitive. If the value does not match a known `LightType`, the filter is silently skipped and all lights are returned.
- `limit` caps results; increase if the scene has many lights.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string lightType = "Point"; // null = all types
        int limit = 50;

        var lights = FindHelper.FindAll<Light>();

        if (!string.IsNullOrEmpty(lightType))
        {
            if (System.Enum.TryParse<LightType>(lightType, true, out var lt))
                lights = lights.Where(l => l.type == lt).ToArray();
        }

        var results = lights.Take(limit).Select(l => new
        {
            name = l.gameObject.name,
            instanceId = l.gameObject.GetInstanceID(),
            path = GameObjectFinder.GetPath(l.gameObject),
            lightType = l.type.ToString(),
            intensity = l.intensity,
            enabled = l.enabled
        }).ToArray();

        result.SetResult(new { count = results.Length, lights = results });
    }
}
```
