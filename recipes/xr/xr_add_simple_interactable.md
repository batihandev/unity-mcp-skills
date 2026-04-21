# xr_add_simple_interactable

Adds XRSimpleInteractable for hover and select events without physical grab physics. Auto-adds a BoxCollider if none exists.

**Signature:** `XRAddSimpleInteractable(name string = null, instanceId int = 0, path string = null)`

**Returns:** `{ success, name, instanceId, interactableType, note }`

**Notes:**
- Does not add a Rigidbody — use `xr_add_grab_interactable` when physics grab is needed.
- Wire up callbacks with `xr_add_interaction_event` after adding this component.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Button";

        var res = UnitySkillsBridge.Call("xr_add_simple_interactable", new { name });
        result.SetResult(res);
    }
}
```
