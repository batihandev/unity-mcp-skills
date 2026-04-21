# xr_setup_continuous_move

Adds continuous stick locomotion to the XR Origin. Tries ActionBasedContinuousMoveProvider first, then falls back to ContinuousMoveProvider.

**Signature:** `XRSetupContinuousMove(name string = null, instanceId int = 0, path string = null, moveSpeed float = 2.0, enableStrafe bool = true, enableFly bool = false)`

**Returns:** `{ success, name, instanceId, providerType, moveSpeed, enableStrafe, enableFly }`

**Notes:**
- Omit all selector parameters to auto-target the XR Origin in the scene.
- `moveSpeed` is in meters per second; comfort default is `2.0`.
- `enableFly`: when true, disables gravity-locked movement.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float moveSpeed = 2f;
        bool enableStrafe = true;
        bool enableFly = false;

        var res = UnitySkillsBridge.Call("xr_setup_continuous_move", new {
            moveSpeed, enableStrafe, enableFly
        });
        result.SetResult(res);
    }
}
```
