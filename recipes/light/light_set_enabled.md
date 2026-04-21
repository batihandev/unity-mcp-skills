# light_set_enabled

Enable or disable a light component.

**Signature:** `LightSetEnabled(string name = null, int instanceId = 0, string path = null, bool enabled = true)`

**Returns:** `{ success, name, enabled }`

**Notes:**
- Provide at least one of `name`, `instanceId`, or `path`.
- This toggles the Light component, not the GameObject itself. Use `gameobject_set_active` to toggle the whole object.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Spot Light";
        int instanceId = 0;
        string path = null;
        bool enabled = false;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var light = go.GetComponent<Light>();
        if (light == null) { result.SetResult(new { error = $"No Light component on {go.name}" }); return; }

        WorkflowManager.SnapshotObject(light);
        Undo.RecordObject(light, "Set Light Enabled");
        light.enabled = enabled;

        result.SetResult(new { success = true, name = go.name, enabled });
    }
}
```
