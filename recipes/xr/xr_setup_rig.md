# xr_setup_rig

Creates a complete XR Origin rig with Camera Offset, Main Camera, Left Controller, and Right Controller, and adds TrackedPoseDriver to each. Also adds XRInteractionManager if none exists.

**Signature:** `XRSetupRig(name string = "XR Origin", x float = 0, y float = 0, z float = 0, cameraYOffset float = 1.36144)`

**Returns:** `{ success, name, instanceId, xriVersion, hierarchy, position, cameraYOffset, note }`

**Notes:**
- Requires `com.unity.xr.core-utils` in addition to XRI.
- `cameraYOffset` sets standing eye height on the Camera Offset child object.
- After this call, add interactors via `xr_add_ray_interactor` or `xr_add_direct_interactor`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        #if !XRI
                    { result.SetResult(NoXRI()); return; }
        #else
                    // Check XROrigin type availability
                    var xrOriginType = XRReflectionHelper.ResolveXRType("XROrigin");
                    if (xrOriginType == null)
                        { result.SetResult(new { error = "XROrigin type not found. Ensure com.unity.xr.core-utils is installed." }); return; }

                    // Root: XR Origin
                    var root = new GameObject(name);
                    root.transform.position = new Vector3(x, y, z);

                    // Add XROrigin component
                    var originComp = root.AddComponent(xrOriginType);
                    if (originComp == null)
                    {
                        UnityEngine.Object.DestroyImmediate(root);
                        { result.SetResult(new { error = "Failed to add XROrigin component." }); return; }
                    }

                    // Camera Offset child
                    var cameraOffset = new GameObject("Camera Offset");
                    cameraOffset.transform.SetParent(root.transform, false);
                    cameraOffset.transform.localPosition = new Vector3(0, cameraYOffset, 0);

                    // Set CameraFloorOffsetObject via reflection
                    XRReflectionHelper.SetProperty(originComp, "CameraFloorOffsetObject", cameraOffset);

                    // Main Camera
                    var camGo = new GameObject("Main Camera");
                    camGo.tag = "MainCamera";
                    camGo.transform.SetParent(cameraOffset.transform, false);
                    var cam = camGo.AddComponent<Camera>();
                    cam.nearClipPlane = 0.01f;
                    camGo.AddComponent<AudioListener>();

                    // Add TrackedPoseDriver to camera
                    var tpdType = FindTrackedPoseDriverType();
                    if (tpdType != null)
                        camGo.AddComponent(tpdType);

                    // Set Camera on XROrigin
                    XRReflectionHelper.SetProperty(originComp, "Camera", cam);

                    // Left Controller
                    var leftCtrl = new GameObject("Left Controller");
                    leftCtrl.transform.SetParent(root.transform, false);
                    if (tpdType != null)
                        leftCtrl.AddComponent(tpdType);

                    // Right Controller
                    var rightCtrl = new GameObject("Right Controller");
                    rightCtrl.transform.SetParent(root.transform, false);
                    if (tpdType != null)
                        rightCtrl.AddComponent(tpdType);

                    // Add XRInteractionManager if none exists
                    var managerComp = XRReflectionHelper.FindFirstOfXRType("XRInteractionManager");
                    if (managerComp == null)
                    {
                        var managerType = XRReflectionHelper.ResolveXRType("XRInteractionManager");
                        if (managerType != null)
                            root.AddComponent(managerType);
                    }

                    Undo.RegisterCreatedObjectUndo(root, "Create XR Origin Rig");
                    WorkflowManager.SnapshotObject(root, SnapshotType.Created);

                    { result.SetResult(new
                    {
                        success = true,
                        name = root.name,
                        instanceId = root.GetInstanceID(),
                        xriVersion = XRReflectionHelper.XRIMajorVersion,
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
        #endif
    }
}
```
