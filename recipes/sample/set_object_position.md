# set_object_position

Set the world-space position of a GameObject.

**Signature:** `SetObjectPosition(string objectName, float x, float y, float z)`

**Returns:** `{ success, name, position: { x, y, z }, message }`

## Notes

- Simplified alternative to `gameobject_set_transform` — prefer the full `gameobject` module for production work.
- Sets `transform.position` (world space).
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
        Undo.RecordObject(obj.transform, "Set Position");
        obj.transform.position = new Vector3(x, y, z);
        { result.SetResult(new { success = true, name = objectName, position = new { x, y, z }, message = $"Set {objectName} position to ({x},{y},{z})" }); return; }
    }
}
```
