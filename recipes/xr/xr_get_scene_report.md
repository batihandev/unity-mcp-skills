# xr_get_scene_report

Generates a comprehensive XR scene diagnostic report listing all XR component types present, their counts, paths, and a summary.

**Signature:** `XRGetSceneReport(verbose bool = false)`

**Returns:** `{ success, xriVersion, totalXRComponents, components, summary }`

**Notes:**
- `summary` contains counts for `interactionManagers`, `origins`, `interactors`, `interactables`, `teleportTargets`.
- Pass `verbose=true` to include a `properties` map for each component entry.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool verbose = false;

        var res = UnitySkillsBridge.Call("xr_get_scene_report", new { verbose });
        result.SetResult(res);
    }
}
```
