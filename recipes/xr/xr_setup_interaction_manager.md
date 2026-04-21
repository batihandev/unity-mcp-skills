# xr_setup_interaction_manager

Adds or finds an XRInteractionManager in the scene. Returns `alreadyExists: true` if one is already present.

**Signature:** `XRSetupInteractionManager(name string = null)`

**Returns:** `{ success, alreadyExists, name, instanceId }`

**Notes:**
- When `name` is null the created object is named `"XR Interaction Manager"`.
- Scenes should normally have exactly one XRInteractionManager.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null; // null defaults to "XR Interaction Manager"

        var res = UnitySkillsBridge.Call("xr_setup_interaction_manager", new { name });
        result.SetResult(res);
    }
}
```
