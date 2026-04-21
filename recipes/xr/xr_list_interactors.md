# xr_list_interactors

Lists all XR interactors in the scene (XRRayInteractor, XRDirectInteractor, XRSocketInteractor, NearFarInteractor) with type, path, and enabled state.

**Signature:** `XRListInteractors(verbose bool = false)`

**Returns:** `{ success, count, interactors, xriVersion }`

**Notes:**
- Pass `verbose=true` to include a `properties` map for each interactor entry.
- Read-only; does not modify the scene.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool verbose = false;

        var res = UnitySkillsBridge.Call("xr_list_interactors", new { verbose });
        result.SetResult(res);
    }
}
```
