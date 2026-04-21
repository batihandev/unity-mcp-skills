# create_sphere

Create a sphere primitive at the specified position.

**Signature:** `CreateSphere(float x = 0, float y = 0, float z = 0, string name = "Sphere")`

**Returns:** `{ success, name, instanceId, position: { x, y, z }, message }`

## Notes

- Simplified alternative to `gameobject_create` with `primitiveType = "Sphere"` — prefer the full `gameobject` module for production work.
- Position is set in world space.
- The created object is registered with Undo.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float x = 0f;
        float y = 0f;
        float z = 0f;
        string name = "Sphere";

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.position = new Vector3(x, y, z);
        Undo.RegisterCreatedObjectUndo(sphere, "Create " + name);
        WorkflowManager.SnapshotObject(sphere, SnapshotType.Created);
        { result.SetResult(new { success = true, name = sphere.name, instanceId = sphere.GetInstanceID(), position = new { x, y, z }, message = $"Created {name} at ({x},{y},{z})" }); return; }
    }
}
```
