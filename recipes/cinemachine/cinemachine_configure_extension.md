# cinemachine_configure_extension

Configure a Cinemachine extension (`CinemachineConfiner`, `CinemachineDeoccluder`/`CinemachineCollider`, `CinemachineFollowZoom`, `CinemachineGroupFraming`, etc.). If `extensionName` is omitted, the first extension found on the VCam is used.

**Signature:** `CinemachineConfigureExtension(string vcamName = null, int instanceId = 0, string path = null, string extensionName = null, string boundingShapeName = null, float? damping = null, float? slowingDistance = null, float? cameraRadius = null, string strategy = null, int? maximumEffort = null, float? smoothingTime = null, float? width = null, float? fovMin = null, float? fovMax = null, string framingMode = null, float? framingSize = null, string sizeAdjustment = null)`

**Returns:** `{ success, extensionType, changes }` or `{ success, extensionType, message }` (no changes) or `{ error }`

**Parameter applicability by extension type:**

Confiner (`CinemachineConfiner`, `CinemachineConfiner2D`, `CinemachineConfiner3D`):
- `boundingShapeName` — name of a GameObject with Collider2D (2D) or Collider (3D)
- `damping` — damping when being confined
- `slowingDistance` — slow-down distance inside bounding shape

Deoccluder / Collider (`CinemachineDeoccluder`, `CinemachineCollider`):
- `cameraRadius`, `strategy`, `maximumEffort`, `smoothingTime`, `damping`

FollowZoom (`CinemachineFollowZoom`):
- `width` — target width in world units
- `fovMin`, `fovMax` — FOV clamp range
- `damping`

GroupFraming (`CinemachineGroupFraming`):
- `framingMode`, `framingSize`, `sizeAdjustment`, `damping`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string vcamName = "My VCam";
        int instanceId = 0;
        string path = null;
        string extensionName = "CinemachineConfiner2D";  // null = auto-detect first extension
        string boundingShapeName = "CameraConfinerShape";
        float? damping = 0f;
        float? slowingDistance = null;
        float? cameraRadius = null;
        string strategy = null;
        int? maximumEffort = null;
        float? smoothingTime = null;
        float? width = null;
        float? fovMin = null;
        float? fovMax = null;
        string framingMode = null;
        float? framingSize = null;
        string sizeAdjustment = null;

        var (go, err) = GameObjectFinder.FindOrError(vcamName, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        MonoBehaviour ext = null;
        if (!string.IsNullOrEmpty(extensionName))
        {
            var extType = CinemachineAdapter.FindCinemachineType(extensionName);
            if (extType != null) ext = go.GetComponent(extType) as MonoBehaviour;
        }
        if (ext == null)
        {
            var exts = go.GetComponents<CinemachineExtension>();
            ext = exts.Length > 0 ? exts[0] : null;
        }
        if (ext == null)
        {
            result.SetResult(new { error = "No Cinemachine extension found. Add one first with cinemachine_add_extension." });
            return;
        }

        WorkflowManager.SnapshotObject(go);
        Undo.RecordObject(ext, "Configure Extension");
        var typeName = ext.GetType().Name;
        var changes = new List<string>();

        if (typeName.Contains("Confiner"))
        {
            if (!string.IsNullOrEmpty(boundingShapeName))
            {
                var shapeGo = GameObjectFinder.Find(boundingShapeName);
                if (shapeGo != null)
                {
                    var col2d = shapeGo.GetComponent<Collider2D>();
                    var col3d = shapeGo.GetComponent<Collider>();
                    if (col2d != null)
                    {
                        var f = ext.GetType().GetField("BoundingShape2D") ?? ext.GetType().GetField("m_BoundingShape2D");
                        f?.SetValue(ext, col2d);
                        changes.Add("boundingShape=" + boundingShapeName + "(2D)");
                    }
                    else if (col3d != null)
                    {
                        var f = ext.GetType().GetField("BoundingVolume") ?? ext.GetType().GetField("m_BoundingVolume");
                        f?.SetValue(ext, col3d);
                        changes.Add("boundingVolume=" + boundingShapeName + "(3D)");
                    }
                }
            }
            if (damping.HasValue)
            {
                var f = ext.GetType().GetField("Damping") ?? ext.GetType().GetField("m_Damping");
                f?.SetValue(ext, damping.Value);
                changes.Add("damping=" + damping.Value);
            }
        }
        else if (typeName.Contains("FollowZoom"))
        {
            if (width.HasValue) { ext.GetType().GetField("Width")?.SetValue(ext, width.Value); changes.Add("width=" + width.Value); }
            if (fovMin.HasValue && fovMax.HasValue)
            {
                ext.GetType().GetField("m_MinFOV")?.SetValue(ext, fovMin.Value);
                ext.GetType().GetField("m_MaxFOV")?.SetValue(ext, fovMax.Value);
                changes.Add("fovRange=(" + fovMin.Value + "," + fovMax.Value + ")");
            }
        }
        else if (typeName.Contains("GroupFraming"))
        {
            if (framingSize.HasValue) { ext.GetType().GetField("FramingSize")?.SetValue(ext, framingSize.Value); changes.Add("framingSize=" + framingSize.Value); }
        }

        EditorUtility.SetDirty(ext);
        if (changes.Count == 0)
            result.SetResult(new { success = true, extensionType = typeName, message = "No changes applied." });
        else
            result.SetResult(new { success = true, extensionType = typeName, changes = string.Join(", ", changes) });
    }
}
```
