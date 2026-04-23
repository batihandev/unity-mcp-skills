# xr_setup_ui_canvas

Converts an existing Canvas to WorldSpace rendering and adds TrackedDeviceGraphicRaycaster. Removes the standard GraphicRaycaster if present. Sets canvas size to 400x300 at scale 0.001.

**Signature:** `XRSetupUICanvas(name string = null, instanceId int = 0, path string = null)`

**Returns:** `{ success, name, instanceId, removedStandardRaycaster, addedTrackedDeviceRaycaster, renderMode }`

**Notes:**
- The target GameObject must already have a Canvas component.
- After conversion, ensure `xr_setup_event_system` has been called and a ray interactor is on the controller.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.UI;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;

        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var canvas = go.GetComponent<Canvas>();
        if (canvas == null)
            { result.SetResult(new { error = $"'{go.name}' does not have a Canvas component." }); return; }

        Undo.RecordObject(go, "Setup XR UI Canvas");

        if (canvas.renderMode != RenderMode.WorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
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
        var existing = go.GetComponent<TrackedDeviceGraphicRaycaster>();
        var trackedRaycaster = existing != null ? existing : go.AddComponent<TrackedDeviceGraphicRaycaster>();
        bool addedTracked = existing == null;

        WorkflowManager.SnapshotObject(go);

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            removedStandardRaycaster = removedStandard,
            addedTrackedDeviceRaycaster = addedTracked,
            renderMode = "WorldSpace"
        }); return; }
    }
}
```
