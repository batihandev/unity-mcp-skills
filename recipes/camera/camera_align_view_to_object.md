# camera_align_view_to_object

Align the Scene View camera to look at a specific GameObject. Resolves by name, instanceId, or path.

**Signature:** `CameraAlignViewToObject(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, message }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Provide at least one of: name, instanceId, or path
        string name = "Player";
        int instanceId = 0;
        string path = null;

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.AlignViewToObject(go.transform);
            result.SetResult(new { success = true, message = $"Aligned view to {go.name}" });
            return;
        }

        result.SetResult(new { error = "No active Scene View found" });
    }
}
```
