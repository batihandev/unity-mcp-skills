# xr_add_interaction_event

Wires a persistent UnityEvent listener from an XR interactable's interaction event to a MonoBehaviour method on another GameObject.

**Signature:** `XRAddInteractionEvent(name string = null, instanceId int = 0, path string = null, eventType string = "selectEntered", targetName string, targetMethod string)`

**Returns:** `{ success, name, instanceId, eventType, targetObject, targetMethod, interactableType }`

**Notes:**
- `targetName` and `targetMethod` are required.
- `eventType` options: `selectEntered`, `selectExited`, `hoverEntered`, `hoverExited`, `firstSelectEntered`, `lastSelectExited`, `firstHoverEntered`, `lastHoverExited`, `activated`, `deactivated`.
- The target method must be public, instance, and parameterless (void) on a MonoBehaviour component of `targetName`.
- Uses `UnityEventTools.AddVoidPersistentListener` — the listener persists across play mode.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Lever";
        string eventType = "selectEntered";
        string targetName = "Door";
        string targetMethod = "Open";

        var res = UnitySkillsBridge.Call("xr_add_interaction_event", new {
            name, eventType, targetName, targetMethod
        });
        result.SetResult(res);
    }
}
```
