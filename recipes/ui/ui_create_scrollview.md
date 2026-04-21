# ui_create_scrollview

Create a ScrollRect (ScrollView) UI element with viewport and content children.

**Signature:** `UICreateScrollview(name string = "ScrollView", parent string = null, width float = 300, height float = 200, horizontal bool = false, vertical bool = true, movementType string = "Elastic")`

**Returns:** `{ success, name, instanceId, parent, horizontal, vertical }`

**Notes:**
- `movementType` accepts `Elastic`, `Clamped`, or `Unrestricted` (case-insensitive via `Enum.TryParse`).
- Content is pre-sized to 400 height; resize with `ui_set_rect` after creation.
- Viewport uses `RectMask2D` for clipping.

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "ScrollView";
        string parent = null;
        float width = 300f, height = 200f;
        bool horizontal = false, vertical = true;
        string movementType = "Elastic";

        var parentGo = FindOrCreateCanvas(parent);
        if (parentGo == null)
        {
            result.SetResult(new { error = "Parent not found and could not create Canvas" });
            return;
        }

        var go = new GameObject(name);
        go.transform.SetParent(parentGo.transform, false);

        var rectTransform = go.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);

        var scrollRect = go.AddComponent<ScrollRect>();
        scrollRect.horizontal = horizontal;
        scrollRect.vertical = vertical;
        if (Enum.TryParse<ScrollRect.MovementType>(movementType, true, out var mt))
            scrollRect.movementType = mt;

        var bgImage = go.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        // Viewport
        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(go.transform, false);
        var viewportRect = viewportGo.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportGo.AddComponent<RectMask2D>();
        var viewportImage = viewportGo.AddComponent<Image>();
        viewportImage.color = new Color(1, 1, 1, 0);

        // Content
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 400);

        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;

        Undo.RegisterCreatedObjectUndo(go, "Create ScrollView");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new { success = true, name = go.name, instanceId = go.GetInstanceID(), parent = parentGo.name, horizontal, vertical });
    }
}
```
