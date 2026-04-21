# xr_setup_turn_provider

Adds snap or smooth continuous turn locomotion to the XR Origin. Tries the ActionBased variant first, then the generic variant.

**Signature:** `XRSetupTurnProvider(name string = null, instanceId int = 0, path string = null, turnType string = "Snap", turnAmount float = 45, turnSpeed float = 90)`

**Returns:** `{ success, name, instanceId, providerType, turnType, turnAmount, turnSpeed }`

**Notes:**
- `turnType` options: `Snap` (default) or `Continuous`.
- `turnAmount` (degrees per snap step) is only applied when `turnType = "Snap"`.
- `turnSpeed` (degrees per second) is only applied when `turnType = "Continuous"`.
- Comfort default: snap turn with `turnAmount = 45`.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string turnType = "Snap";
        float turnAmount = 45f;
        float turnSpeed = 90f;

        var res = UnitySkillsBridge.Call("xr_setup_turn_provider", new {
            turnType, turnAmount, turnSpeed
        });
        result.SetResult(res);
    }
}
```
