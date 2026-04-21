# set_object_rotation

Set the rotation of a GameObject using Euler angles.

**Signature:** `SetObjectRotation(string objectName, float x, float y, float z)`

**Returns:** `{ success, name, rotation: { x, y, z }, message }`

## Notes

- Simplified alternative to `gameobject_set_transform` — prefer the full `gameobject` module for production work.
- Angles are Euler degrees applied via `Quaternion.Euler(x, y, z)`.
- The change is registered with Undo.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string objectName = "ObjectName"; // required
        float x = 0f;
        float y = 0f;
        float z = 0f;

        var (obj, err) = GameObjectFinder.FindOrError(objectName);
        if (err != null) { result.SetResult(err); return; }
        Undo.RecordObject(obj.transform, "Set Rotation");
        obj.transform.rotation = Quaternion.Euler(x, y, z);
        { result.SetResult(new { success = true, name = objectName, rotation = new { x, y, z }, message = $"Set {objectName} rotation to ({x},{y},{z})" }); return; }
    }
}
```
