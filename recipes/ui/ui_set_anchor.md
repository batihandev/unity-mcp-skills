# ui_set_anchor

Apply an anchor preset to a UI element's RectTransform.

**Signature:** `UISetAnchor(name string = null, instanceId int = 0, path string = null, preset string = "MiddleCenter", setPivot bool = true)`

**Returns:** `{ success, name, preset, anchorMin, anchorMax }`

**Notes:**
- Preset values: `TopLeft`, `TopCenter`, `TopRight`, `MiddleLeft`, `MiddleCenter`, `MiddleRight`, `BottomLeft`, `BottomCenter`, `BottomRight`, `StretchHorizontal`, `StretchVertical`, `StretchAll`.
- Spaces in preset names are stripped before matching.
- Returns an error for unknown preset names.

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
        string preset = "MiddleCenter";
        bool setPivot = true;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var rect = go.GetComponent<RectTransform>();
        if (rect == null) { result.SetResult(new { error = "GameObject has no RectTransform" }); return; }

        WorkflowManager.SnapshotObject(rect);
        Undo.RecordObject(rect, "Set Anchor");

        Vector2 anchorMin, anchorMax, pivot;
        switch (preset.ToLower().Replace(" ", ""))
        {
            case "topleft":
                anchorMin = anchorMax = new Vector2(0, 1); pivot = new Vector2(0, 1); break;
            case "topcenter":
                anchorMin = anchorMax = new Vector2(0.5f, 1); pivot = new Vector2(0.5f, 1); break;
            case "topright":
                anchorMin = anchorMax = new Vector2(1, 1); pivot = new Vector2(1, 1); break;
            case "middleleft":
                anchorMin = anchorMax = new Vector2(0, 0.5f); pivot = new Vector2(0, 0.5f); break;
            case "middlecenter":
                anchorMin = anchorMax = new Vector2(0.5f, 0.5f); pivot = new Vector2(0.5f, 0.5f); break;
            case "middleright":
                anchorMin = anchorMax = new Vector2(1, 0.5f); pivot = new Vector2(1, 0.5f); break;
            case "bottomleft":
                anchorMin = anchorMax = new Vector2(0, 0); pivot = new Vector2(0, 0); break;
            case "bottomcenter":
                anchorMin = anchorMax = new Vector2(0.5f, 0); pivot = new Vector2(0.5f, 0); break;
            case "bottomright":
                anchorMin = anchorMax = new Vector2(1, 0); pivot = new Vector2(1, 0); break;
            case "stretchhorizontal":
                anchorMin = new Vector2(0, 0.5f); anchorMax = new Vector2(1, 0.5f); pivot = new Vector2(0.5f, 0.5f); break;
            case "stretchvertical":
                anchorMin = new Vector2(0.5f, 0); anchorMax = new Vector2(0.5f, 1); pivot = new Vector2(0.5f, 0.5f); break;
            case "stretchall":
                anchorMin = Vector2.zero; anchorMax = Vector2.one; pivot = new Vector2(0.5f, 0.5f); break;
            default:
                result.SetResult(new { error = $"Unknown anchor preset: {preset}" });
                return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        if (setPivot) rect.pivot = pivot;

        result.SetResult(new { success = true, name = go.name, preset, anchorMin = $"({anchorMin.x}, {anchorMin.y})", anchorMax = $"({anchorMax.x}, {anchorMax.y})" });
    }
}
```
