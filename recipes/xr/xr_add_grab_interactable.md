# xr_add_grab_interactable

Makes a GameObject grabbable by adding XRGrabInteractable. Auto-adds Rigidbody and Collider if missing. The collider must not be a trigger.

**Signature:** `XRAddGrabInteractable(name string = null, instanceId int = 0, path string = null, movementType string = "VelocityTracking", throwOnDetach bool = true, smoothPosition bool = true, smoothRotation bool = true, smoothPositionAmount float = 5, smoothRotationAmount float = 5, useGravity bool = true, isKinematic bool = false, attachTransformOffset string = null)`

**Returns:** `{ success, name, instanceId, movementType, throwOnDetach, smoothPosition, smoothRotation, movementTypeOptions }`

**Notes:**
- `movementType` options: `VelocityTracking` (default, best for physics), `Kinematic` (handles/tools), `Instantaneous` (precise remote grab).
- `attachTransformOffset`: comma-separated `"x,y,z"` string that creates a child Attach Point transform.
- If the GameObject has a MeshFilter with a mesh, a convex MeshCollider is added; otherwise a BoxCollider.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.xr.interaction.toolkit` (≥ 3.4).

```csharp
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string movementType = "VelocityTracking";
        bool throwOnDetach = true;
        bool smoothPosition = true;
        bool smoothRotation = true;
        float smoothPositionAmount = 5f;
        float smoothRotationAmount = 5f;
        bool useGravity = true;
        bool isKinematic = false;
        string attachTransformOffset = null;

        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) { result.SetResult(findErr); return; }

        Undo.RecordObject(go, "Add XRGrabInteractable");

        // Ensure Rigidbody
        var rb = go.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = go.AddComponent<Rigidbody>();
            rb.useGravity = useGravity;
            rb.isKinematic = isKinematic;
        }

        // Ensure Collider
        if (go.GetComponent<Collider>() == null)
        {
            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
                go.AddComponent<MeshCollider>().convex = true;
            else
                go.AddComponent<BoxCollider>();
        }

        // Add XRGrabInteractable
        var existing = go.GetComponent<XRGrabInteractable>();
        var comp = existing != null ? existing : go.AddComponent<XRGrabInteractable>();
        if (existing == null)
            Undo.RegisterCreatedObjectUndo(comp, "Add XRGrabInteractable");

        // Configure directly
        if (Enum.TryParse<XRBaseInteractable.MovementType>(movementType, true, out var mt))
            comp.movementType = mt;
        comp.throwOnDetach = throwOnDetach;
        comp.smoothPosition = smoothPosition;
        comp.smoothRotation = smoothRotation;
        comp.smoothPositionAmount = smoothPositionAmount;
        comp.smoothRotationAmount = smoothRotationAmount;

        // Create and set custom attach transform if offset specified
        if (!string.IsNullOrEmpty(attachTransformOffset))
        {
            var parts = attachTransformOffset.Split(',');
            if (parts.Length == 3 &&
                float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ox) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var oy) &&
                float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var oz))
            {
                var attachGo = new GameObject("Attach Point");
                attachGo.transform.SetParent(go.transform, false);
                attachGo.transform.localPosition = new Vector3(ox, oy, oz);
                comp.attachTransform = attachGo.transform;
            }
        }

        WorkflowManager.SnapshotObject(go);

        { result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            movementType,
            throwOnDetach,
            smoothPosition,
            smoothRotation,
            movementTypeOptions = Enum.GetNames(typeof(XRBaseInteractable.MovementType))
        }); return; }
    }
}
```
