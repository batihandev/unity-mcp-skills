# camera_set_properties

Set one or more Game Camera properties. Pass only the parameters you want to change; omitted parameters are left unchanged. To change FOV, pass `fieldOfView` — there is no `camera_set_fov` command.

**Signature:** `CameraSetProperties(string name = null, int instanceId = 0, string path = null, float? fieldOfView = null, float? nearClipPlane = null, float? farClipPlane = null, float? depth = null, string clearFlags = null, float? bgR = null, float? bgG = null, float? bgB = null)`

**Returns:** `{ success, name }`

Valid `clearFlags` values: `Skybox`, `SolidColor`, `Depth`, `Nothing`

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

        // Only set the values you want to change; leave others null
        float? fieldOfView = 60f;
        float? nearClipPlane = null;
        float? farClipPlane = null;
        float? depth = null;
        string clearFlags = null;
        float? bgR = null;
        float? bgG = null;
        float? bgB = null;

        var (cam, err) = GameObjectFinder.FindComponentOrError<Camera>(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        WorkflowManager.SnapshotObject(cam);
        Undo.RecordObject(cam, "Set Camera Properties");

        if (fieldOfView.HasValue) cam.fieldOfView = fieldOfView.Value;
        if (nearClipPlane.HasValue) cam.nearClipPlane = nearClipPlane.Value;
        if (farClipPlane.HasValue) cam.farClipPlane = farClipPlane.Value;
        if (depth.HasValue) cam.depth = depth.Value;
        if (!string.IsNullOrEmpty(clearFlags) && System.Enum.TryParse<CameraClearFlags>(clearFlags, true, out var cf))
            cam.clearFlags = cf;
        if (bgR.HasValue || bgG.HasValue || bgB.HasValue)
        {
            var c = cam.backgroundColor;
            cam.backgroundColor = new Color(bgR ?? c.r, bgG ?? c.g, bgB ?? c.b, c.a);
        }

        result.SetResult(new { success = true, name = cam.gameObject.name });
    }
}
```
