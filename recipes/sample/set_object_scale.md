# set_object_scale

Set the local scale of a GameObject.

**Signature:** `SetObjectScale(string objectName, float x, float y, float z)`

**Returns:** `{ success, name, scale: { x, y, z }, message }`

## Notes

- Simplified alternative to `gameobject_set_transform` — prefer the full `gameobject` module for production work.
- Sets `transform.localScale`.
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
        float x = 1f;
        float y = 1f;
        float z = 1f;

        var (obj, err) = GameObjectFinder.FindOrError(objectName);
        if (err != null) { result.SetResult(err); return; }
        Undo.RecordObject(obj.transform, "Set Scale");
        obj.transform.localScale = new Vector3(x, y, z);
        { result.SetResult(new { success = true, name = objectName, scale = new { x, y, z }, message = $"Set {objectName} scale to ({x},{y},{z})" }); return; }
    }
}
```
