# xr_add_teleport_area

Marks a surface as a teleport destination by adding TeleportationArea. Auto-adds a Collider if none exists.

**Signature:** `XRAddTeleportArea(name string = null, instanceId int = 0, path string = null, matchOrientation string = "WorldSpaceUp")`

**Returns:** `{ success, name, instanceId, teleportType, matchOrientation, matchOrientationOptions }`

**Notes:**
- `matchOrientation` options: `WorldSpaceUp` (default), `TargetUp`, `TargetUpAndForward`, `None`.
- If a MeshFilter with a mesh is present, a MeshCollider is added; otherwise a BoxCollider.
- The collider must NOT be a trigger — the raycast hits it as a surface.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string matchOrientation = "WorldSpaceUp";

        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) { result.SetResult(findErr); return; }

        Undo.RecordObject(go, "Add TeleportationArea");

        var existing = go.GetComponent<TeleportationArea>();
        var comp = existing != null ? existing : go.AddComponent<TeleportationArea>();
        if (existing == null)
            Undo.RegisterCreatedObjectUndo(comp, "Add TeleportationArea");

        if (!string.IsNullOrEmpty(matchOrientation) &&
            Enum.TryParse<MatchOrientation>(matchOrientation, true, out var mo))
            comp.matchOrientation = mo;

        // Ensure collider for raycast detection
        if (go.GetComponent<Collider>() == null)
        {
            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
                go.AddComponent<MeshCollider>();
            else
                go.AddComponent<BoxCollider>();
        }

        WorkflowManager.SnapshotObject(go);

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            teleportType = "TeleportationArea",
            matchOrientation,
            matchOrientationOptions = Enum.GetNames(typeof(MatchOrientation))
        }); return; }
    }
}
```
