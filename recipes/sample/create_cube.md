# create_cube

Create a cube primitive at the specified position.

**Signature:** `CreateCube(float x = 0, float y = 0, float z = 0, string name = "Cube")`

**Returns:** `{ success, name, instanceId, position: { x, y, z }, message }`

## Notes

- Simplified alternative to `gameobject_create` with `primitiveType = "Cube"` — prefer the full `gameobject` module for production work.
- Position is set in world space.
- The created object is registered with Undo.

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
        string name = "Cube";

        /* Original Logic:

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = new Vector3(x, y, z);
            Undo.RegisterCreatedObjectUndo(cube, "Create " + name);
            WorkflowManager.SnapshotObject(cube, SnapshotType.Created);
            return new { success = true, name = cube.name, instanceId = cube.GetInstanceID(), position = new { x, y, z }, message = $"Created {name} at ({x},{y},{z})" };
        */
    }
}
```
