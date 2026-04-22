# cinemachine_configure_body

Configure the Body stage component (Follow, OrbitalFollow, ThirdPersonFollow, PositionComposer, FramingTransposer, etc.) in one call. Only fields matching the active Body component type are applied.

**Signature:** `CinemachineConfigureBody(string vcamName = null, int instanceId = 0, string path = null, float? offsetX = null, float? offsetY = null, float? offsetZ = null, string bindingMode = null, float? dampingX = null, float? dampingY = null, float? dampingZ = null, string orbitStyle = null, float? radius = null, float? topHeight = null, float? topRadius = null, float? midHeight = null, float? midRadius = null, float? bottomHeight = null, float? bottomRadius = null, float? shoulderX = null, float? shoulderY = null, float? shoulderZ = null, float? verticalArmLength = null, float? cameraSide = null, float? cameraDistance = null, float? screenX = null, float? screenY = null, float? deadZoneWidth = null, float? deadZoneHeight = null)`

**Returns:** `{ success, componentType, changes }` or `{ success, componentType, message }` (no changes) or `{ error }`

**Parameter applicability by component:**

| Parameter | Follow/Transposer | OrbitalFollow | ThirdPersonFollow | PositionComposer/FramingTransposer |
|-----------|:-----------------:|:-------------:|:-----------------:|:----------------------------------:|
| offsetX/Y/Z | CM3 | - | - | - |
| bindingMode | CM3 | CM3 | - | - |
| dampingX/Y/Z | CM3/CM2 | CM3/CM2 | CM3 | CM3/CM2 |
| orbitStyle, radius | - | CM3 | - | - |
| top/mid/bottomHeight/Radius | - | CM3 | - | - |
| shoulderX/Y/Z | - | - | CM3/CM2 | - |
| verticalArmLength, cameraSide | - | - | CM3/CM2 | - |
| cameraDistance | - | - | CM3 | CM3/CM2 |
| screenX/Y | - | - | - | CM3/CM2 |
| deadZoneWidth/Height | - | - | - | CM3/CM2 |

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
        // Follow offset (CM3 CinemachineFollow)
        float? offsetX = null;
        float? offsetY = 2f;
        float? offsetZ = -5f;
        string bindingMode = null;
        float? dampingX = null;
        float? dampingY = null;
        float? dampingZ = null;
        // OrbitalFollow (CM3)
        string orbitStyle = null;
        float? radius = null;
        float? topHeight = null; float? topRadius = null;
        float? midHeight = null; float? midRadius = null;
        float? bottomHeight = null; float? bottomRadius = null;
        // ThirdPersonFollow
        float? shoulderX = null; float? shoulderY = null; float? shoulderZ = null;
        float? verticalArmLength = null;
        float? cameraSide = null;
        // PositionComposer / FramingTransposer
        float? cameraDistance = null;
        float? screenX = null; float? screenY = null;
        float? deadZoneWidth = null; float? deadZoneHeight = null;

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        CinemachineComponentBase body = null;
        foreach (var c in go.GetComponents<CinemachineComponentBase>())
        {
            if (c != null && c.Stage == CinemachineCore.Stage.Body) { body = c; break; }
        }
        if (body == null)
        {
            result.SetResult(new { error = "No Body stage component found. Add one first." });
            return;
        }

        WorkflowManager.SnapshotObject(go);
        Undo.RecordObject(body, "Configure Body");
        var typeName = body.GetType().Name;
        var changes = new List<string>();

        if (typeName == "CinemachineFollow" && (offsetX.HasValue || offsetY.HasValue || offsetZ.HasValue))
        {
            var f = (CinemachineFollow)body;
            var cur = f.FollowOffset;
            f.FollowOffset = new Vector3(offsetX ?? cur.x, offsetY ?? cur.y, offsetZ ?? cur.z);
            changes.Add("offset=(" + f.FollowOffset.x + "," + f.FollowOffset.y + "," + f.FollowOffset.z + ")");
        }

        EditorUtility.SetDirty(body);
        if (changes.Count == 0)
            result.SetResult(new { success = true, componentType = typeName, message = "No changes applied (parameters may not match this component type)." });
        else
            result.SetResult(new { success = true, componentType = typeName, changes = string.Join(", ", changes) });
    }
}
```

**Tip:** For complex Body configurations, use `cinemachine_set_vcam_property` with dot-notation paths (e.g. `propertyName = "TrackerSettings.BindingMode"`).
