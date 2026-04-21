# xr_add_ray_interactor

Adds XRRayInteractor, LineRenderer, and optionally XRInteractorLineVisual to a controller GameObject for remote pointing and ray-based interaction.

**Signature:** `XRAddRayInteractor(name string = null, instanceId int = 0, path string = null, maxDistance float = 30, lineType string = "StraightLine", addLineVisual bool = true)`

**Returns:** `{ success, name, instanceId, interactorType, maxRaycastDistance, lineType, hasLineVisual, lineTypeOptions }`

**Notes:**
- `lineType` options: `StraightLine`, `ProjectileCurve`, `BezierCurve`.
- A LineRenderer is auto-added with `startWidth=0.01`, `endWidth=0.01` if none exists.
- XRRayInteractor does not require a Collider.

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

                    Undo.RecordObject(go, "Add XRRayInteractor");

                    // Add XRRayInteractor
                    var comp = XRReflectionHelper.AddXRComponent(go, "XRRayInteractor");
                    if (comp == null)
                        { result.SetResult(new { error = "Failed to add XRRayInteractor. Type not found in current XRI version." }); return; }

                    Undo.RegisterCreatedObjectUndo(comp, "Add XRRayInteractor");

                    // Configure properties
                    XRReflectionHelper.SetProperty(comp, "maxRaycastDistance", maxDistance);
                    if (!string.IsNullOrEmpty(lineType))
                        XRReflectionHelper.SetEnumProperty(comp, "lineType", lineType);

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
                    Component lineVisual = null;
                    if (addLineVisual)
                    {
                        lineVisual = XRReflectionHelper.AddXRComponent(go, "XRInteractorLineVisual");
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
                        lineTypeOptions = XRReflectionHelper.GetEnumValues(comp, "lineType")
                    }); return; }
        #endif
    }
}
```
