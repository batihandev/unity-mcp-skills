# cinemachine_configure_aim

Configure the Aim stage component (RotationComposer, Composer, PanTilt, POV, etc.) in one call. Only fields matching the active Aim component are applied.

**Signature:** `CinemachineConfigureAim(string vcamName = null, int instanceId = 0, string path = null, float? screenX = null, float? screenY = null, float? deadZoneWidth = null, float? deadZoneHeight = null, float? softZoneWidth = null, float? softZoneHeight = null, float? horizontalDamping = null, float? verticalDamping = null, float? lookaheadTime = null, float? lookaheadSmoothing = null, bool? centerOnActivate = null, string referenceFrame = null, float? panValue = null, float? tiltValue = null, float? targetOffsetX = null, float? targetOffsetY = null, float? targetOffsetZ = null)`

**Returns:** `{ success, componentType, changes }` or `{ success, componentType, message }` (no changes) or `{ error }`

**Parameter applicability:**

Composer / RotationComposer:
- `screenX`, `screenY` — target on-screen position (0–1)
- `deadZoneWidth`, `deadZoneHeight` — dead zone (no correction inside)
- `softZoneWidth`, `softZoneHeight` — soft zone (gradual correction) — CM2 only
- `horizontalDamping`, `verticalDamping` — composition damping
- `lookaheadTime`, `lookaheadSmoothing` — target lookahead
- `centerOnActivate` — re-center on camera activation
- `targetOffsetX/Y/Z` — offset from target pivot

PanTilt / POV:
- `referenceFrame` — rotation reference frame (CM3)
- `panValue`, `tiltValue` — explicit axis values

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Cinemachine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string vcamName = "My VCam";
        int instanceId = 0;
        string path = null;
        // Composer / RotationComposer
        float? screenX = 0.5f;
        float? screenY = 0.5f;
        float? deadZoneWidth = 0.1f;
        float? deadZoneHeight = 0.1f;
        float? softZoneWidth = null;
        float? softZoneHeight = null;
        float? horizontalDamping = 0.5f;
        float? verticalDamping = 0.5f;
        float? lookaheadTime = null;
        float? lookaheadSmoothing = null;
        bool? centerOnActivate = null;
        // PanTilt / POV
        string referenceFrame = null;
        float? panValue = null;
        float? tiltValue = null;
        // Target offset
        float? targetOffsetX = null;
        float? targetOffsetY = null;
        float? targetOffsetZ = null;

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        CinemachineComponentBase aim = null;
        foreach (var c in go.GetComponents<CinemachineComponentBase>())
        {
            if (c != null && c.Stage == CinemachineCore.Stage.Aim) { aim = c; break; }
        }
        if (aim == null)
        {
            result.SetResult(new { error = "No Aim stage component found. Add one first (e.g. CinemachineRotationComposer, CinemachinePanTilt)." });
            return;
        }

        WorkflowManager.SnapshotObject(go);
        Undo.RecordObject(aim, "Configure Aim");
        var typeName = aim.GetType().Name;
        var changes = new List<string>();

        if (typeName.Contains("Composer"))
        {
            var rc = (CinemachineRotationComposer)aim;
            if (screenX.HasValue || screenY.HasValue)
            {
                var cur = rc.Composition.ScreenPosition;
                var pos = new Vector2(screenX ?? cur.x, screenY ?? cur.y);
                var comp = rc.Composition;
                comp.ScreenPosition = pos;
                rc.Composition = comp;
                changes.Add("screen=(" + pos.x + "," + pos.y + ")");
            }
        }
        else if (typeName.Contains("PanTilt") || typeName.Contains("POV"))
        {
            if (referenceFrame != null)
            {
                var f = aim.GetType().GetField("ReferenceFrame");
                if (f != null)
                {
                    f.SetValue(aim, System.Enum.Parse(f.FieldType, referenceFrame, true));
                    changes.Add("referenceFrame=" + referenceFrame);
                }
            }
        }

        EditorUtility.SetDirty(aim);
        if (changes.Count == 0)
            result.SetResult(new { success = true, componentType = typeName, message = "No changes applied." });
        else
            result.SetResult(new { success = true, componentType = typeName, changes = string.Join(", ", changes) });
    }
}
```

**Tip:** For full control, use `cinemachine_set_vcam_property` with dot-notation (e.g. `componentType="RotationComposer"`, `propertyName="Composition.ScreenPosition"`).
