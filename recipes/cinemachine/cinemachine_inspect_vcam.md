# cinemachine_inspect_vcam

Deeply inspect a VCam, returning name, priority, Follow/LookAt targets, lens settings, and all Cinemachine pipeline components with their serialized fields.

**Signature:** `CinemachineInspectVCam(string vcamName = null, int instanceId = 0, string path = null)`

**Returns:** `{ name, priority, follow, lookAt, lens, components[] }`

**Notes:**
- Provide at least one of `vcamName`, `instanceId`, or `path`.
- `components` is an array of objects with `_type`, `settings`, and optionally `stage` (`Body`, `Aim`, `Noise`, `Extension`).
- Read-only — does not modify anything.

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string vcamName = "My VCam";
        int instanceId = 0;
        string path = null;

        var (go, err) = GameObjectFinder.FindOrError(name: vcamName, instanceId: instanceId, path: path);
        if (err != null) { result.SetResult(err); return; }

        var vcam = CinemachineAdapter.GetVCam(go);
        if (CinemachineAdapter.VCamOrError(vcam) is object vcamErr) { result.SetResult(vcamErr); return; }

        var followName = CinemachineAdapter.GetFollow(vcam) ? CinemachineAdapter.GetFollow(vcam).name : "None";
        var lookAtName = CinemachineAdapter.GetLookAt(vcam) ? CinemachineAdapter.GetLookAt(vcam).name : "None";
        var priority = CinemachineAdapter.GetPriority(vcam);
        var lens = CinemachineAdapter.GetLens(vcam);

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
