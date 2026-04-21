# xr_check_setup

Validates XR package installation, rig presence, managers, event system, collider rules, and TrackedPoseDriver on controllers.

**Signature:** `XRCheckSetup(verbose bool = false)`

**Returns:** `{ xriInstalled, xriMajorVersion, interactionManagerCount, xrOriginCount, mainCamera, eventSystemCount, hasXRUIInputModule, interactorCount, interactableCount, hasTeleportation, hasContinuousMove, hasTurnProvider, issues, issueCount, success }`

**Notes:**
- Returns `{ error: "XR Interaction Toolkit package ... is not installed" }` if XRI is missing.
- Pass `verbose=true` to include `interactorDetails` and `interactableDetails` arrays.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool verbose = false;

        var res = UnitySkillsBridge.Call("xr_check_setup", new { verbose });
        result.SetResult(res);
    }
}
```
