# camera_set_orthographic

Switch a Game Camera between orthographic and perspective projection. Optionally set the orthographic size at the same time.

**Signature:** `CameraSetOrthographic(bool orthographic, float? orthographicSize = null, string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, orthographic, orthographicSize }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool orthographic = true;
        float? orthographicSize = 5f; // only relevant when orthographic = true

        // Provide at least one of: name, instanceId, or path
        string name = "Main Camera";
        int instanceId = 0;
        string path = null;

        var (cam, err) = GameObjectFinder.FindComponentOrError<Camera>(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        WorkflowManager.SnapshotObject(cam);
        Undo.RecordObject(cam, "Set Orthographic");

        cam.orthographic = orthographic;
        if (orthographicSize.HasValue) cam.orthographicSize = orthographicSize.Value;

        result.SetResult(new { success = true, orthographic, orthographicSize = cam.orthographicSize });
    }
}
```
