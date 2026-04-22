# camera_get_info

Get the Scene View camera's current position, rotation, pivot, size, and projection mode. Read-only; no parameters required.

**Signature:** `CameraGetInfo()`

**Returns:** `{ position: {x,y,z}, rotation: {x,y,z}, pivot: {x,y,z}, size, orthographic }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if (SceneView.lastActiveSceneView != null)
        {
            var cam = SceneView.lastActiveSceneView.camera;
            result.SetResult(new
            {
                position = new { x = cam.transform.position.x, y = cam.transform.position.y, z = cam.transform.position.z },
                rotation = new { x = cam.transform.eulerAngles.x, y = cam.transform.eulerAngles.y, z = cam.transform.eulerAngles.z },
                pivot = new { x = SceneView.lastActiveSceneView.pivot.x, y = SceneView.lastActiveSceneView.pivot.y, z = SceneView.lastActiveSceneView.pivot.z },
                size = SceneView.lastActiveSceneView.size,
                orthographic = SceneView.lastActiveSceneView.orthographic
            });
            return;
        }
        result.SetResult(new { error = "No active Scene View found" });
    }
}
```
