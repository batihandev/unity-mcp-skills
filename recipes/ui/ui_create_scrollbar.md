# ui_create_scrollbar

Create a standalone Scrollbar UI element with a sliding area and handle.

**Signature:** `UICreateScrollbar(name string = "Scrollbar", parent string = null, direction string = "BottomToTop", value float = 0, size float = 0.2, numberOfSteps int = 0)`

**Returns:** `{ success, name, instanceId, parent, direction }`

**Notes:**
- `direction` values: `BottomToTop`, `TopToBottom`, `LeftToRight`, `RightToLeft`.
- Size is automatically set to 160x20 for horizontal directions and 20x160 for vertical.
- `numberOfSteps = 0` means continuous (no snapping).

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Scrollbar";
        string parent = null;
        string direction = "BottomToTop";
        float value = 0f, size = 0.2f;
        int numberOfSteps = 0;

        var parentGo = FindOrCreateCanvas(parent);
        if (parentGo == null)
        {
            result.SetResult(new { error = "Parent not found and could not create Canvas" });
            return;
        }

        var go = new GameObject(name);
        go.transform.SetParent(parentGo.transform, false);

        var rectTransform = go.AddComponent<RectTransform>();
        var isHorizontal = direction.Contains("Left") || direction.Contains("Right");
        rectTransform.sizeDelta = isHorizontal ? new Vector2(160, 20) : new Vector2(20, 160);

        var bgImage = go.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.8f, 0.8f, 0.8f);

        // Sliding Area
        var slideAreaGo = new GameObject("Sliding Area");
        slideAreaGo.transform.SetParent(go.transform, false);
        var slideAreaRect = slideAreaGo.AddComponent<RectTransform>();
        slideAreaRect.anchorMin = Vector2.zero;
        slideAreaRect.anchorMax = Vector2.one;
        slideAreaRect.offsetMin = new Vector2(10, 10);
        slideAreaRect.offsetMax = new Vector2(-10, -10);

        // Handle
        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(slideAreaGo.transform, false);
        var handleRect = handleGo.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 20);
        var handleImage = handleGo.AddComponent<UnityEngine.UI.Image>();
        handleImage.color = Color.white;

        var scrollbar = go.AddComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.value = value;
        scrollbar.size = size;
        scrollbar.numberOfSteps = numberOfSteps;

        if (Enum.TryParse<Scrollbar.Direction>(direction, true, out var dir))
            scrollbar.direction = dir;

        Undo.RegisterCreatedObjectUndo(go, "Create Scrollbar");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new { success = true, name = go.name, instanceId = go.GetInstanceID(), parent = parentGo.name, direction });
    }

    private static GameObject FindOrCreateCanvas(string parentName)
    {
        if (!string.IsNullOrEmpty(parentName))
        {
            var p = GameObject.Find(parentName);
            if (p != null) return p;
        }
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null) return canvas.gameObject;
        var go = new GameObject("Canvas");
        var canvasComp = go.AddComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);
        return go;
    }
}
```
