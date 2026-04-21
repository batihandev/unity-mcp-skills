# cinemachine_set_spline

Assign a `SplineContainer` to a VCam's `CinemachineSplineDolly` component (Body stage). **CM3 + Unity Splines package only.**

**Signature:** `CinemachineSetSpline(string vcamName = null, int vcamInstanceId = 0, string vcamPath = null, string splineName = null, int splineInstanceId = 0, string splinePath = null)`

**Returns:** `{ success, message }` or `{ error }`

**Prerequisites:**
- CM3 (`Unity.Cinemachine`) must be installed.
- The `com.unity.splines` package must be installed.
- The VCam must already have `CinemachineSplineDolly` on the Body stage. Use `cinemachine_set_component` with `stage="Body"`, `componentType="CinemachineSplineDolly"` first.
- The spline GameObject must have a `SplineContainer` component.

```csharp
using UnityEngine;
using UnityEditor;

#if CINEMACHINE_3 && HAS_SPLINES
using Unity.Cinemachine;
using UnityEngine.Splines;
#endif

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

#if CINEMACHINE_3 && HAS_SPLINES
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
#elif CINEMACHINE_2
        result.SetResult(new { error = "cinemachine_set_spline requires Cinemachine 3.x + Splines package." });
#else
        result.SetResult(new { error = "Cinemachine 3.x and/or the Splines package (com.unity.splines) is not installed." });
#endif
    }
}
```
