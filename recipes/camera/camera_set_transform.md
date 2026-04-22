# camera_set_transform

Set the Scene View camera's position and rotation explicitly. Controls the **Scene View** camera, not a Game Camera.

**Signature:** `CameraSetTransform(float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float size = 5f, bool instant = true)`

**Returns:** `{ success, message }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float posX = 0f;
        float posY = 5f;
        float posZ = -10f;
        float rotX = 15f;
        float rotY = 0f;
        float rotZ = 0f;
        float size = 5f;
        bool instant = true;

        if (SceneView.lastActiveSceneView != null)
        {
            var sceneView = SceneView.lastActiveSceneView;
            var position = new Vector3(posX, posY, posZ);
            var rotation = Quaternion.Euler(rotX, rotY, rotZ);

            sceneView.LookAt(position, rotation, size);

            result.SetResult(new { success = true, message = "Scene View camera updated" });
            return;
        }
        result.SetResult(new { error = "No active Scene View found" });
    }
}
```
