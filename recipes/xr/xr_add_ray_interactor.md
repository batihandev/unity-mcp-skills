# xr_add_ray_interactor

Adds XRRayInteractor, LineRenderer, and optionally XRInteractorLineVisual to a controller GameObject for remote pointing and ray-based interaction.

**Signature:** `XRAddRayInteractor(name string = null, instanceId int = 0, path string = null, maxDistance float = 30, lineType string = "StraightLine", addLineVisual bool = true)`

**Returns:** `{ success, name, instanceId, interactorType, maxRaycastDistance, lineType, hasLineVisual, lineTypeOptions }`

**Notes:**
- `lineType` options: `StraightLine`, `ProjectileCurve`, `BezierCurve`.
- A LineRenderer is auto-added with `startWidth=0.01`, `endWidth=0.01` if none exists.
- XRRayInteractor does not require a Collider.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        float maxDistance = 30f;
        string lineType = "StraightLine";
        bool addLineVisual = true;

        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) { result.SetResult(findErr); return; }

        Undo.RecordObject(go, "Add XRRayInteractor");

        var existing = go.GetComponent<XRRayInteractor>();
        var comp = existing != null ? existing : go.AddComponent<XRRayInteractor>();
        if (existing == null)
            Undo.RegisterCreatedObjectUndo(comp, "Add XRRayInteractor");

        // Configure properties
        comp.maxRaycastDistance = maxDistance;
        if (!string.IsNullOrEmpty(lineType) &&
            Enum.TryParse<XRRayInteractor.LineType>(lineType, true, out var lt))
            comp.lineType = lt;

        // Add LineRenderer if not present
        var lr = go.GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = go.AddComponent<LineRenderer>();
            lr.startWidth = 0.01f;
            lr.endWidth = 0.01f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.white;
            lr.endColor = new Color(1, 1, 1, 0.5f);
        }

        // Add XRInteractorLineVisual if requested
        XRInteractorLineVisual lineVisual = null;
        if (addLineVisual)
        {
            lineVisual = go.GetComponent<XRInteractorLineVisual>() ?? go.AddComponent<XRInteractorLineVisual>();
        }

        WorkflowManager.SnapshotObject(go);

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            interactorType = comp.GetType().Name,
            maxRaycastDistance = maxDistance,
            lineType,
            hasLineVisual = lineVisual != null,
            lineTypeOptions = Enum.GetNames(typeof(XRRayInteractor.LineType))
        }); return; }
    }
}
```
