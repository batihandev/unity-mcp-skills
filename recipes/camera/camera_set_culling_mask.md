# camera_set_culling_mask

Set a Game Camera's culling mask using comma-separated layer names. Replaces the entire mask; list all layers that should remain visible.

**Signature:** `CameraSetCullingMask(string layerNames, string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, cullingMask }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Comma-separated layer names, e.g. "Default,UI,Water"
        string layerNames = "Default,UI";

        // Provide at least one of: name, instanceId, or path
        string name = "Main Camera";
        int instanceId = 0;
        string path = null;

        var (cam, err) = GameObjectFinder.FindComponentOrError<Camera>(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        WorkflowManager.SnapshotObject(cam);
        Undo.RecordObject(cam, "Set Culling Mask");

        int mask = 0;
        foreach (var ln in layerNames.Split(','))
        {
            var layer = LayerMask.NameToLayer(ln.Trim());
            if (layer >= 0) mask |= 1 << layer;
        }
        cam.cullingMask = mask;
        result.SetResult(new { success = true, cullingMask = mask });
    }
}
```
