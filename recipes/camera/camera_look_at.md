# camera_look_at

Focus the Scene View camera on a world-space point. Preserves the current rotation and size. To focus on a named object, use `camera_align_view_to_object` instead.

**Signature:** `CameraLookAt(float x, float y, float z)`

**Returns:** `{ success }`

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

        if (SceneView.lastActiveSceneView != null)
        {
            var sceneView = SceneView.lastActiveSceneView;
            sceneView.LookAt(new Vector3(x, y, z), sceneView.rotation, sceneView.size);
            result.SetResult(new { success = true });
            return;
        }
        result.SetResult(new { error = "No active Scene View found" });
    }
}
```
