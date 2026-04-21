# camera_get_properties

Get all properties of a Game Camera component. Resolves by name, instanceId, or path.

**Signature:** `CameraGetProperties(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, name, fieldOfView, nearClipPlane, farClipPlane, orthographic, orthographicSize, depth, cullingMask, clearFlags, backgroundColor: {r,g,b,a}, rect: {x,y,w,h} }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Provide at least one of: name, instanceId, or path
        string name = "Main Camera";
        int instanceId = 0;
        string path = null;

        var (cam, err) = GameObjectFinder.FindComponentOrError<Camera>(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        result.SetResult(new
        {
            success = true,
            name = cam.gameObject.name,
            fieldOfView = cam.fieldOfView,
            nearClipPlane = cam.nearClipPlane,
            farClipPlane = cam.farClipPlane,
            orthographic = cam.orthographic,
            orthographicSize = cam.orthographicSize,
            depth = cam.depth,
            cullingMask = cam.cullingMask,
            clearFlags = cam.clearFlags.ToString(),
            backgroundColor = new { r = cam.backgroundColor.r, g = cam.backgroundColor.g, b = cam.backgroundColor.b, a = cam.backgroundColor.a },
            rect = new { x = cam.rect.x, y = cam.rect.y, w = cam.rect.width, h = cam.rect.height }
        });
    }
}
```
