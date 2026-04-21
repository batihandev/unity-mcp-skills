# xr_configure_interaction_layers

Sets the InteractionLayerMask on an XR interactor or interactable for filtering which objects can interact with each other.

**Signature:** `XRConfigureInteractionLayers(name string = null, instanceId int = 0, path string = null, layers string = "Default", isInteractor bool = true)`

**Returns:** `{ success, name, instanceId, componentType, layers, isInteractor }`

**Notes:**
- `layers`: comma-separated XR interaction layer names, e.g. `"Default,Teleport"`.
- `isInteractor = true` targets an interactor component; `false` targets an interactable component.
- Uses `InteractionLayerMask.GetMask()` via reflection. Falls back to integer parsing if the type is unavailable.
- Do not confuse XR InteractionLayerMask with Unity physics Layer — they are separate systems.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "TeleportRay";
        string layers = "Teleport";
        bool isInteractor = true;

        var res = UnitySkillsBridge.Call("xr_configure_interaction_layers", new {
            name, layers, isInteractor
        });
        result.SetResult(res);
    }
}
```
