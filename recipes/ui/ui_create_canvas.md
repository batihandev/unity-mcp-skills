# ui_create_canvas

Create a new Canvas with the specified render mode.

**Signature:** `UICreateCanvas(name string = "Canvas", renderMode string = "ScreenSpaceOverlay")`

**Returns:** `{ success, name, instanceId, renderMode }`

**Notes:**
- `renderMode` accepts `ScreenSpaceOverlay`, `ScreenSpaceCamera`, or `WorldSpace` (case-insensitive).
- Automatically adds `CanvasScaler` and `GraphicRaycaster` components.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Canvas";
        string renderMode = "ScreenSpaceOverlay";

        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        switch (renderMode.ToLower())
        {
            case "screenspaceoverlay":
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                break;
            case "screenspacecamera":
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                break;
            case "worldspace":
                canvas.renderMode = RenderMode.WorldSpace;
                break;
            default:
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                break;
        }

        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            renderMode = canvas.renderMode.ToString()
        });
    }
}
```
