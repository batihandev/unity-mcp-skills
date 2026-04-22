# xr_setup_rig

Creates a complete XR Origin rig with Camera Offset, Main Camera, Left Controller, and Right Controller, and adds TrackedPoseDriver to each. Also adds XRInteractionManager if none exists.

**Signature:** `XRSetupRig(name string = "XR Origin", x float = 0, y float = 0, z float = 0, cameraYOffset float = 1.36144)`

**Returns:** `{ success, name, instanceId, xriVersion, hierarchy, position, cameraYOffset, note }`

**Notes:**
- Requires `com.unity.xr.core-utils` (XROrigin) and `com.unity.inputsystem` (TrackedPoseDriver).
- `cameraYOffset` sets standing eye height on the Camera Offset child object.
- After this call, add interactors via `xr_add_ray_interactor` or `xr_add_direct_interactor`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4), `com.unity.xr.core-utils`, `com.unity.inputsystem`.

```csharp
using UnityEngine;
using UnityEditor;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "XR Origin";
        float x = 0f, y = 0f, z = 0f;
        float cameraYOffset = 1.36144f;

        // Root: XR Origin
        var root = new GameObject(name);
        root.transform.position = new Vector3(x, y, z);

        var originComp = root.AddComponent<XROrigin>();

        // Camera Offset child
        var cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(root.transform, false);
        cameraOffset.transform.localPosition = new Vector3(0, cameraYOffset, 0);
        originComp.CameraFloorOffsetObject = cameraOffset;

        // Main Camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(cameraOffset.transform, false);
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.01f;
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<TrackedPoseDriver>();
        originComp.Camera = cam;

        // Left Controller
        var leftCtrl = new GameObject("Left Controller");
        leftCtrl.transform.SetParent(root.transform, false);
        leftCtrl.AddComponent<TrackedPoseDriver>();

        // Right Controller
        var rightCtrl = new GameObject("Right Controller");
        rightCtrl.transform.SetParent(root.transform, false);
        rightCtrl.AddComponent<TrackedPoseDriver>();

        // Add XRInteractionManager if none exists
        var manager = UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();
        if (manager == null)
            root.AddComponent<XRInteractionManager>();

        Undo.RegisterCreatedObjectUndo(root, "Create XR Origin Rig");
        WorkflowManager.SnapshotObject(root, SnapshotType.Created);

        { result.SetResult(new
        {
            success = true,
            name = root.name,
            instanceId = root.GetInstanceID(),
            xriVersion = 3,
            hierarchy = new
            {
                cameraOffset = cameraOffset.name,
                mainCamera = camGo.name,
                leftController = leftCtrl.name,
                rightController = rightCtrl.name
            },
            position = new { x, y, z },
            cameraYOffset,
            note = "Add interactors to controllers via xr_add_ray_interactor or xr_add_direct_interactor."
        }); return; }
    }
}
```
