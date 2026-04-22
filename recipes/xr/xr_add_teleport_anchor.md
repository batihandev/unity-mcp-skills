# xr_add_teleport_anchor

Creates a stationary teleport destination at a specific position and rotation. Adds TeleportationAnchor, a thin BoxCollider for raycast detection, and a flat cylinder visual indicator.

**Signature:** `XRAddTeleportAnchor(name string = "Teleport Anchor", x float = 0, y float = 0, z float = 0, rotY float = 0, matchOrientation string = "TargetUpAndForward", parent string = null)`

**Returns:** `{ success, name, instanceId, teleportType, position, rotationY, matchOrientation }`

**Notes:**
- `matchOrientation` defaults to `TargetUpAndForward` to face the player toward the anchor's forward direction.
- The BoxCollider has size `(1, 0.01, 1)` — a flat detection surface at floor level.
- The cylinder visual child ("Anchor Visual") has its auto-generated collider removed and is scaled `(1, 0.02, 1)`.
- `parent`: name of a GameObject to parent the anchor under (optional).

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
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Teleport Anchor";
        float x = 0f, y = 0f, z = 0f;
        float rotY = 0f;
        string matchOrientation = "TargetUpAndForward";
        string parent = null;

        var go = new GameObject(name);
        go.transform.position = new Vector3(x, y, z);
        go.transform.rotation = Quaternion.Euler(0, rotY, 0);

        if (!string.IsNullOrEmpty(parent))
        {
            var parentGo = GameObjectFinder.Find(parent);
            if (parentGo != null)
                go.transform.SetParent(parentGo.transform, true);
        }

        var comp = go.AddComponent<TeleportationAnchor>();

        if (!string.IsNullOrEmpty(matchOrientation) &&
            Enum.TryParse<MatchOrientation>(matchOrientation, true, out var mo))
            comp.matchOrientation = mo;

        // Add a small collider for raycast detection
        var collider = go.AddComponent<BoxCollider>();
        collider.size = new Vector3(1, 0.01f, 1);

        // Add visual indicator
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Anchor Visual";
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = new Vector3(1, 0.02f, 1);
        var renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            renderer.sharedMaterial.color = new Color(0, 0.8f, 1, 0.5f);
        }
        // Remove auto-generated collider from visual primitive
        var visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
            UnityEngine.Object.DestroyImmediate(visualCollider);

        Undo.RegisterCreatedObjectUndo(go, "Create Teleport Anchor");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            teleportType = "TeleportationAnchor",
            position = new { x, y, z },
            rotationY = rotY,
            matchOrientation
        }); return; }
    }
}
```
