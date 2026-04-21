# xr_setup_event_system

Finds or creates an EventSystem, removes StandaloneInputModule if present, and adds XRUIInputModule for XR-compatible UI input.

**Signature:** `XRSetupEventSystem()`

**Returns:** `{ success, name, instanceId, created, removedStandaloneInputModule, addedXRUIInputModule }`

**Notes:**
- Safe to call even if EventSystem already exists — it only modifies the input modules.
- Required before XR UI Canvas interactions work correctly.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var res = UnitySkillsBridge.Call("xr_setup_event_system", new { });
        result.SetResult(res);
    }
}
```
