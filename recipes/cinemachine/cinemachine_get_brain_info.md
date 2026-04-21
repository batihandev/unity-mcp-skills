# cinemachine_get_brain_info

Get information about the active `CinemachineBrain` on the Main Camera: which VCam is active, whether a blend is in progress, and the update method.

**Signature:** `CinemachineGetBrainInfo()`

**Returns:** `{ success, activeCamera, isBlending, activeBlend, updateMethod }` or `{ error }`

**Notes:**
- Read-only — does not modify anything.
- Returns an error if there is no Main Camera or no `CinemachineBrain` on the Main Camera.
- `activeBlend` is `"None"` when no blend is in progress.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var mainCamera = Camera.main;
        if (mainCamera == null) { result.SetResult(new { error = "No Main Camera" }); return; }

        var brain = mainCamera.GetComponent<CinemachineBrain>();
        if (brain == null) { result.SetResult(new { error = "No CinemachineBrain on Main Camera" }); return; }

        var activeCam = brain.ActiveVirtualCamera as Component;
        var updateMethod = CinemachineAdapter.GetBrainUpdateMethod(brain);

        result.SetResult(new
        {
            success = true,
            activeCamera = activeCam ? activeCam.name : "None",
            isBlending = brain.IsBlending,
            activeBlend = brain.ActiveBlend?.Description ?? "None",
            updateMethod
        });
    }
}
```
