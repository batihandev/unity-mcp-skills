# xr_add_ray_interactor

Adds XRRayInteractor, LineRenderer, and optionally XRInteractorLineVisual to a controller GameObject for remote pointing and ray-based interaction.

**Signature:** `XRAddRayInteractor(name string = null, instanceId int = 0, path string = null, maxDistance float = 30, lineType string = "StraightLine", addLineVisual bool = true)`

**Returns:** `{ success, name, instanceId, interactorType, maxRaycastDistance, lineType, hasLineVisual, lineTypeOptions }`

**Notes:**
- `lineType` options: `StraightLine`, `ProjectileCurve`, `BezierCurve`.
- A LineRenderer is auto-added with `startWidth=0.01`, `endWidth=0.01` if none exists.
- XRRayInteractor does not require a Collider.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Right Controller";
        float maxDistance = 30f;
        string lineType = "StraightLine";
        bool addLineVisual = true;

        var res = UnitySkillsBridge.Call("xr_add_ray_interactor", new {
            name, maxDistance, lineType, addLineVisual
        });
        result.SetResult(res);
    }
}
```
