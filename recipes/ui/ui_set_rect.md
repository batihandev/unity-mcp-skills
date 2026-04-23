# ui_set_rect

Set RectTransform size, anchored position, and padding offsets.

**Signature:** `UISetRect(name string = null, instanceId int = 0, path string = null, width float? = null, height float? = null, posX float? = null, posY float? = null, left float? = null, right float? = null, top float? = null, bottom float? = null)`

**Returns:** `{ success, name, sizeDelta, anchoredPosition }`

**Notes:**
- All size/position/offset parameters are optional; only provided values are modified.
- `left`/`bottom` control `offsetMin`; `right`/`top` control `offsetMax` (stored as negatives).
- Use after `ui_set_anchor` for precise placement of stretched elements.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        float? width = null, height = null;
        float? posX = null, posY = null;
        float? left = null, right = null, top = null, bottom = null;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var rect = go.GetComponent<RectTransform>();
        if (rect == null) { result.SetResult(new { error = "GameObject has no RectTransform" }); return; }

        WorkflowManager.SnapshotObject(rect);
        Undo.RecordObject(rect, "Set Rect");

        // Size
        if (width.HasValue || height.HasValue)
        {
            var size = rect.sizeDelta;
            if (width.HasValue) size.x = width.Value;
            if (height.HasValue) size.y = height.Value;
            rect.sizeDelta = size;
        }

        // Position
        if (posX.HasValue || posY.HasValue)
        {
            var pos = rect.anchoredPosition;
            if (posX.HasValue) pos.x = posX.Value;
            if (posY.HasValue) pos.y = posY.Value;
            rect.anchoredPosition = pos;
        }

        // Offsets (padding for stretched elements)
        if (left.HasValue || bottom.HasValue)
        {
            var min = rect.offsetMin;
            if (left.HasValue) min.x = left.Value;
            if (bottom.HasValue) min.y = bottom.Value;
            rect.offsetMin = min;
        }
        if (right.HasValue || top.HasValue)
        {
            var max = rect.offsetMax;
            if (right.HasValue) max.x = -right.Value;
            if (top.HasValue) max.y = -top.Value;
            rect.offsetMax = max;
        }

        result.SetResult(new { success = true, name = go.name, sizeDelta = $"({rect.sizeDelta.x}, {rect.sizeDelta.y})", anchoredPosition = $"({rect.anchoredPosition.x}, {rect.anchoredPosition.y})" });
    }
}
```
