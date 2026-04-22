# cinemachine_inspect_vcam

Deeply inspect a VCam, returning name, priority, Follow/LookAt targets, lens settings, and all Cinemachine pipeline components with their serialized fields.

**Signature:** `CinemachineInspectVCam(string vcamName = null, int instanceId = 0, string path = null)`

**Returns:** `{ name, priority, follow, lookAt, lens, components[] }`

**Notes:**
- Provide at least one of `vcamName`, `instanceId`, or `path`.
- `components` is an array of objects with `_type`, `settings`, and optionally `stage` (`Body`, `Aim`, `Noise`, `Extension`).
- Read-only — does not modify anything.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string vcamName = "My VCam";
        int instanceId = 0;
        string path = null;

        var (go, err) = GameObjectFinder.FindOrError(name: vcamName, instanceId: instanceId, path: path);
        if (err != null) { result.SetResult(err); return; }

        var vcam = go.GetComponent<CinemachineCamera>();
        if (vcam == null) { result.SetResult(new { error = "Not a CinemachineCamera" }); return; }

        var followName = vcam.Follow ? vcam.Follow.name : "None";
        var lookAtName = vcam.LookAt ? vcam.LookAt.name : "None";
        var priority = vcam.Priority.Value;
        var lens = vcam.Lens;

        result.SetResult(new
        {
            name = vcam.name,
            priority,
            follow = followName,
            lookAt = lookAtName,
            lens,
        });
    }
}
```
