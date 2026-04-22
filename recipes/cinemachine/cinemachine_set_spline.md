# cinemachine_set_spline

Assign a `SplineContainer` to a `CinemachineCamera`'s `CinemachineSplineDolly` component (Body stage).

**Signature:** `CinemachineSetSpline(string vcamName = null, int vcamInstanceId = 0, string vcamPath = null, string splineName = null, int splineInstanceId = 0, string splinePath = null)`

**Returns:** `{ success, message }` or `{ error }`

**Prerequisites:**
- The `com.unity.splines` package must be installed alongside `com.unity.cinemachine`.
- The VCam must already have `CinemachineSplineDolly` on the Body stage. Use `cinemachine_set_component` with `stage="Body"`, `componentType="CinemachineSplineDolly"` first.
- The spline GameObject must have a `SplineContainer` component.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;
using UnityEngine.Splines;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string vcamName = "My VCam";
        int vcamInstanceId = 0;
        string vcamPath = null;
        string splineName = "My Spline";
        int splineInstanceId = 0;
        string splinePath = null;

        var (vcamGo, vcamErr) = GameObjectFinder.FindOrError(vcamName, vcamInstanceId, vcamPath);
        if (vcamErr != null) { result.SetResult(vcamErr); return; }

        var vcam = vcamGo.GetComponent<CinemachineCamera>();
        if (vcam == null) { result.SetResult(new { error = "Not a CinemachineCamera" }); return; }

        var dolly = vcam.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineSplineDolly;
        if (dolly == null)
        {
            result.SetResult(new { error = "VCam does not have a CinemachineSplineDolly on Body stage. Use cinemachine_set_component first." });
            return;
        }

        var (splineGo, splineErr) = GameObjectFinder.FindOrError(splineName, splineInstanceId, splinePath);
        if (splineErr != null) { result.SetResult(splineErr); return; }

        var container = splineGo.GetComponent<SplineContainer>();
        if (container == null) { result.SetResult(new { error = "GameObject does not have a SplineContainer" }); return; }

        WorkflowManager.SnapshotObject(vcamGo);
        Undo.RecordObject(dolly, "Set Spline");
        dolly.Spline = container;

        result.SetResult(new { success = true, message = "Assigned Spline " + splineGo.name + " to VCam " + vcamGo.name });
    }
}
```
