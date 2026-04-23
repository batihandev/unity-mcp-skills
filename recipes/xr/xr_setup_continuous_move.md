# xr_setup_continuous_move

Adds continuous stick locomotion to the XR Origin via ContinuousMoveProvider.

**Signature:** `XRSetupContinuousMove(name string = null, instanceId int = 0, path string = null, moveSpeed float = 2.0, enableStrafe bool = true, enableFly bool = false)`

**Returns:** `{ success, name, instanceId, providerType, moveSpeed, enableStrafe, enableFly }`

**Notes:**
- Omit all selector parameters to auto-target the XR Origin in the scene.
- `moveSpeed` is in meters per second; comfort default is `2.0`.
- `enableFly`: when true, disables gravity-locked movement.
- XRI 3 collapsed `ActionBasedContinuousMoveProvider` into `ContinuousMoveProvider`; this recipe uses the unified type.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        float moveSpeed = 2f;
        bool enableStrafe = true;
        bool enableFly = false;

        GameObject go;
        if (string.IsNullOrEmpty(name) && instanceId == 0 && string.IsNullOrEmpty(path))
        {
            var origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (origin == null)
                { result.SetResult(new { error = "No XR Origin found in scene. Create one via xr_setup_rig, or specify the target object." }); return; }
            go = origin.gameObject;
        }
        else
        {
            var (found, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (findErr != null) { result.SetResult(findErr); return; }
            go = found;
        }

        Undo.RecordObject(go, "Setup Continuous Move");

        var existing = go.GetComponent<ContinuousMoveProvider>();
        var comp = existing != null ? existing : go.AddComponent<ContinuousMoveProvider>();
        if (existing == null)
            Undo.RegisterCreatedObjectUndo(comp, "Add ContinuousMoveProvider");

        comp.moveSpeed = moveSpeed;
        comp.enableStrafe = enableStrafe;
        comp.enableFly = enableFly;

        WorkflowManager.SnapshotObject(go);

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            providerType = comp.GetType().Name,
            moveSpeed,
            enableStrafe,
            enableFly
        }); return; }
    }
}
```
