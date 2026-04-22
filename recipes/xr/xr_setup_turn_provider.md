# xr_setup_turn_provider

Adds snap or smooth continuous turn locomotion to the XR Origin.

**Signature:** `XRSetupTurnProvider(name string = null, instanceId int = 0, path string = null, turnType string = "Snap", turnAmount float = 45, turnSpeed float = 90)`

**Returns:** `{ success, name, instanceId, providerType, turnType, turnAmount, turnSpeed }`

**Notes:**
- `turnType` options: `Snap` (default) or `Continuous`.
- `turnAmount` (degrees per snap step) is only applied when `turnType = "Snap"`.
- `turnSpeed` (degrees per second) is only applied when `turnType = "Continuous"`.
- Comfort default: snap turn with `turnAmount = 45`.
- XRI 3 unified the ActionBased and non-action variants; this recipe uses `SnapTurnProvider` / `ContinuousTurnProvider` directly.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using System;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string turnType = "Snap";
        float turnAmount = 45f;
        float turnSpeed = 90f;

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

        Undo.RecordObject(go, "Setup Turn Provider");

        bool isSnap = turnType.Equals("Snap", StringComparison.OrdinalIgnoreCase);
        Behaviour comp;

        if (isSnap)
        {
            var existing = go.GetComponent<SnapTurnProvider>();
            var snap = existing != null ? existing : go.AddComponent<SnapTurnProvider>();
            if (existing == null)
                Undo.RegisterCreatedObjectUndo(snap, "Add SnapTurnProvider");
            snap.turnAmount = turnAmount;
            comp = snap;
        }
        else
        {
            var existing = go.GetComponent<ContinuousTurnProvider>();
            var cont = existing != null ? existing : go.AddComponent<ContinuousTurnProvider>();
            if (existing == null)
                Undo.RegisterCreatedObjectUndo(cont, "Add ContinuousTurnProvider");
            cont.turnSpeed = turnSpeed;
            comp = cont;
        }

        WorkflowManager.SnapshotObject(go);

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            providerType = comp.GetType().Name,
            turnType,
            turnAmount = isSnap ? turnAmount : 0f,
            turnSpeed = isSnap ? 0f : turnSpeed
        }); return; }
    }
}
```
