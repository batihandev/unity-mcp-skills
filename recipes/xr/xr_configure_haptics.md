# xr_configure_haptics

Sets haptic feedback intensity and duration for select and hover events on an XR interactor.

**Signature:** `XRConfigureHaptics(name string = null, instanceId int = 0, path string = null, selectIntensity float = 0.5, selectDuration float = 0.1, hoverIntensity float = 0.1, hoverDuration float = 0.05)`

**Returns:** `{ success, name, instanceId, interactorType, changedProperties, selectIntensity, selectDuration, hoverIntensity, hoverDuration, note }`

**Notes:**
- Properties are set via reflection; `changedProperties` lists only those successfully applied.
- If `changedProperties` is empty, the haptic API may differ in the installed XRI version.
- Works on XRRayInteractor, XRDirectInteractor, XRSocketInteractor, or XRBaseInteractor.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Right Controller";
        float selectIntensity = 0.7f;
        float selectDuration = 0.15f;
        float hoverIntensity = 0.1f;
        float hoverDuration = 0.05f;

        var res = UnitySkillsBridge.Call("xr_configure_haptics", new {
            name, selectIntensity, selectDuration, hoverIntensity, hoverDuration
        });
        result.SetResult(res);
    }
}
```
