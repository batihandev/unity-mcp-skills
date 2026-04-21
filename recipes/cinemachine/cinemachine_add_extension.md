# cinemachine_add_extension

Add a `CinemachineExtension` component to a VCam. If the extension already exists on the VCam, the call succeeds without adding a duplicate.

**Signature:** `CinemachineAddExtension(string vcamName = null, int instanceId = 0, string path = null, string extensionName = null)`

**Returns:** `{ success, message }` or `{ error }`

**Common extensionName values:**
- `"CinemachineImpulseListener"` — receive camera shake impulses
- `"CinemachineConfiner2D"` / `"CinemachineConfiner"` — constrain camera to a 2D/3D volume
- `"CinemachineDeoccluder"` / `"CinemachineCollider"` — avoid occlusion by obstacles
- `"CinemachineFollowZoom"` — auto-zoom to keep group in frame
- `"CinemachineGroupFraming"` — frame a target group
- `"CinemachineStoryboard"` — overlay a storyboard image

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string vcamName = "My VCam";
        int instanceId = 0;
        string path = null;
        string extensionName = "CinemachineImpulseListener";

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        var vcam = CinemachineAdapter.GetVCam(go);
        if (CinemachineAdapter.VCamOrError(vcam) is object vcamErr) { result.SetResult(vcamErr); return; }

        var type = CinemachineAdapter.FindCinemachineType(extensionName);
        if (type == null) { result.SetResult(new { error = "Could not find Cinemachine extension type: " + extensionName }); return; }
        if (!typeof(CinemachineExtension).IsAssignableFrom(type))
        {
            result.SetResult(new { error = type.Name + " is not a CinemachineExtension" });
            return;
        }

        if (go.GetComponent(type) != null)
        {
            result.SetResult(new { success = true, message = "Extension " + type.Name + " already exists on " + go.name });
            return;
        }

        WorkflowManager.SnapshotObject(go);
        var ext = Undo.AddComponent(go, type);
        if (ext == null) { result.SetResult(new { error = "Failed to add extension " + type.Name }); return; }
        WorkflowManager.SnapshotCreatedComponent(ext);

        result.SetResult(new { success = true, message = "Added extension " + type.Name });
    }
}
```
