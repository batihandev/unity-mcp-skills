# xr_setup_ui_canvas

Converts an existing Canvas to WorldSpace rendering and adds TrackedDeviceGraphicRaycaster. Removes the standard GraphicRaycaster if present. Sets canvas size to 400x300 at scale 0.001.

**Signature:** `XRSetupUICanvas(name string = null, instanceId int = 0, path string = null)`

**Returns:** `{ success, name, instanceId, removedStandardRaycaster, addedTrackedDeviceRaycaster, renderMode, note }`

**Notes:**
- The target GameObject must already have a Canvas component.
- After conversion, ensure `xr_setup_event_system` has been called and a ray interactor is on the controller.
- `note` is set if TrackedDeviceGraphicRaycaster could not be found (XRI UI module may be missing).

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
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
                    var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
                    if (findErr != null) { result.SetResult(findErr); return; }

                    var canvas = go.GetComponent<Canvas>();
                    if (canvas == null)
                        { result.SetResult(new { error = $"'{go.name}' does not have a Canvas component." }); return; }

                    Undo.RecordObject(go, "Setup XR UI Canvas");

                    // Set Canvas to WorldSpace for XR
                    if (canvas.renderMode != RenderMode.WorldSpace)
                    {
                        canvas.renderMode = RenderMode.WorldSpace;
                        // Set reasonable defaults for world-space XR canvas
                        var rt = canvas.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.sizeDelta = new Vector2(400, 300);
                            rt.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                        }
                    }

                    // Remove standard GraphicRaycaster
                    var standardRaycaster = go.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                    bool removedStandard = false;
                    if (standardRaycaster != null)
                    {
                        Undo.DestroyObjectImmediate(standardRaycaster);
                        removedStandard = true;
                    }

                    // Add TrackedDeviceGraphicRaycaster
                    var trackedRaycaster = XRReflectionHelper.AddXRComponent(go, "TrackedDeviceGraphicRaycaster");
                    bool addedTracked = trackedRaycaster != null;

                    WorkflowManager.SnapshotObject(go);

                    { result.SetResult(new
                    {
                        success = true,
                        name = go.name,
                        instanceId = go.GetInstanceID(),
                        removedStandardRaycaster = removedStandard,
                        addedTrackedDeviceRaycaster = addedTracked,
                        renderMode = "WorldSpace",
                        note = addedTracked ? null : "TrackedDeviceGraphicRaycaster type not found. Ensure XRI UI module is installed."
                    }); return; }
        #endif
    }
}
```
