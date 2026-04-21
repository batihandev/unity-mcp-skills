# set_object_scale

Set the local scale of a GameObject.

**Signature:** `SetObjectScale(string objectName, float x, float y, float z)`

**Returns:** `{ success, name, scale: { x, y, z }, message }`

## Notes

- Simplified alternative to `gameobject_set_transform` — prefer the full `gameobject` module for production work.
- Sets `transform.localScale`.
- The change is registered with Undo.

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

        /* Original Logic:

            var (obj, err) = GameObjectFinder.FindOrError(objectName);
            if (err != null) return err;
            Undo.RecordObject(obj.transform, "Set Scale");
            obj.transform.localScale = new Vector3(x, y, z);
            return new { success = true, name = objectName, scale = new { x, y, z }, message = $"Set {objectName} scale to ({x},{y},{z})" };
        */
    }
}
```
