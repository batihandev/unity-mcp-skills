# xr_add_grab_interactable

Makes a GameObject grabbable by adding XRGrabInteractable. Auto-adds Rigidbody and Collider if missing. The collider must not be a trigger.

**Signature:** `XRAddGrabInteractable(name string = null, instanceId int = 0, path string = null, movementType string = "VelocityTracking", throwOnDetach bool = true, smoothPosition bool = true, smoothRotation bool = true, smoothPositionAmount float = 5, smoothRotationAmount float = 5, useGravity bool = true, isKinematic bool = false, attachTransformOffset string = null)`

**Returns:** `{ success, name, instanceId, movementType, throwOnDetach, smoothPosition, smoothRotation, movementTypeOptions }`

**Notes:**
- `movementType` options: `VelocityTracking` (default, best for physics), `Kinematic` (handles/tools), `Instantaneous` (precise remote grab).
- `attachTransformOffset`: comma-separated `"x,y,z"` string that creates a child Attach Point transform.
- If the GameObject has a MeshFilter with a mesh, a convex MeshCollider is added; otherwise a BoxCollider.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        #if !XRI
                    { result.SetResult(NoXRI()); return; }
        #else
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
                        // Auto-detect best collider based on mesh
                        var meshFilter = go.GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                            go.AddComponent<MeshCollider>().convex = true;
                        else
                            go.AddComponent<BoxCollider>();
                    }

                    // Add XRGrabInteractable
                    var comp = XRReflectionHelper.AddXRComponent(go, "XRGrabInteractable");
                    if (comp == null)
                        { result.SetResult(new { error = "Failed to add XRGrabInteractable. Type not found in current XRI version." }); return; }

                    Undo.RegisterCreatedObjectUndo(comp, "Add XRGrabInteractable");

                    // Configure via reflection
                    XRReflectionHelper.SetEnumProperty(comp, "movementType", movementType);
                    XRReflectionHelper.SetProperty(comp, "throwOnDetach", throwOnDetach);
                    XRReflectionHelper.SetProperty(comp, "smoothPosition", smoothPosition);
                    XRReflectionHelper.SetProperty(comp, "smoothRotation", smoothRotation);
                    XRReflectionHelper.SetProperty(comp, "smoothPositionAmount", smoothPositionAmount);
                    XRReflectionHelper.SetProperty(comp, "smoothRotationAmount", smoothRotationAmount);

                    // Create and set custom attach transform if offset specified
                    if (!string.IsNullOrEmpty(attachTransformOffset))
                    {
                        var offsets = ParseVector3(attachTransformOffset);
                        if (offsets.HasValue)
                        {
                            var attachGo = new GameObject("Attach Point");
                            attachGo.transform.SetParent(go.transform, false);
                            attachGo.transform.localPosition = offsets.Value;
                            XRReflectionHelper.SetProperty(comp, "attachTransform", attachGo.transform);
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
                        movementTypeOptions = XRReflectionHelper.GetEnumValues(comp, "movementType")
                    }); return; }
        #endif
    }
}
```
