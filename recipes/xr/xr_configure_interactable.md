# xr_configure_interactable

Fine-tunes properties on an existing XR interactable (XRGrabInteractable or XRSimpleInteractable). Pass only the fields you want to change.

**Signature:** `XRConfigureInteractable(name string = null, instanceId int = 0, path string = null, selectMode string = null, movementType string = null, throwOnDetach bool? = null, smoothPosition bool? = null, smoothRotation bool? = null, smoothPositionAmount float? = null, smoothRotationAmount float? = null, trackPosition bool? = null, trackRotation bool? = null)`

**Returns:** `{ success, name, instanceId, interactableType, changedProperties, selectModeOptions, movementTypeOptions }`

**Notes:**
- All parameters except the target selector are optional — omit unchanged fields.
- `changedProperties` lists only the properties that were actually updated via reflection.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyCube";
        string movementType = "Kinematic";
        bool? throwOnDetach = false;

        var res = UnitySkillsBridge.Call("xr_configure_interactable", new {
            name, movementType, throwOnDetach
        });
        result.SetResult(res);
    }
}
```
