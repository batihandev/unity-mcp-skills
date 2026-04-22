# cinemachine_set_lens

Quickly configure Lens settings on a Virtual Camera: Field of View, Near/Far Clip Planes, and Orthographic Size.

**Signature:** `CinemachineSetLens(string vcamName = null, int instanceId = 0, string path = null, float? fov = null, float? nearClip = null, float? farClip = null, float? orthoSize = null, string mode = null)`

**Returns:** `{ success, message }` or `{ error }`

**Notes:**
- Supply only the parameters you want to change; omitted parameters are left as-is.
- `mode` is accepted but currently unused by the upstream implementation; it is reserved for future lens mode switching.
- At least one of `fov`, `nearClip`, `farClip`, or `orthoSize` must be supplied.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string vcamName = "My VCam";
        int instanceId = 0;
        string path = null;
        float? fov = 60f;
        float? nearClip = 0.3f;
        float? farClip = 1000f;
        float? orthoSize = null;

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        WorkflowManager.SnapshotObject(go);

        var vcam = go.GetComponent<CinemachineCamera>();
        if (vcam == null) { result.SetResult(new { error = "Not a CinemachineCamera" }); return; }

        var lens = vcam.Lens;
        bool changed = false;

        if (fov.HasValue) { lens.FieldOfView = fov.Value; changed = true; }
        if (nearClip.HasValue) { lens.NearClipPlane = nearClip.Value; changed = true; }
        if (farClip.HasValue) { lens.FarClipPlane = farClip.Value; changed = true; }
        if (orthoSize.HasValue) { lens.OrthographicSize = orthoSize.Value; changed = true; }

        if (changed)
        {
            vcam.Lens = lens;
            EditorUtility.SetDirty(go);
            result.SetResult(new { success = true, message = "Updated Lens settings" });
        }
        else
        {
            result.SetResult(new { error = "No values provided to update." });
        }
    }
}
```
