# ui_align_selected

Align the currently selected UI elements along a common edge or axis.

**Signature:** `UIAlignSelected(alignment string = "Center")`

**Returns:** `{ success, alignment, count }`

**Notes:**
- Requires at least 2 selected GameObjects with a `RectTransform`.
- `alignment` values: `Left`, `Center`, `Right` (horizontal axis) or `Top`, `Middle`, `Bottom` (vertical axis).
- `Center` aligns to the average X position; `Middle` aligns to the average Y position.

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string alignment = "Center";

        var selected = Selection.gameObjects.Where(g => g.GetComponent<RectTransform>() != null).ToList();
        if (selected.Count < 2)
        {
            result.SetResult(new { error = "Select at least 2 UI elements" });
            return;
        }

        Undo.RecordObjects(selected.Select(g => g.GetComponent<RectTransform>()).Cast<UnityEngine.Object>().ToArray(), "Align UI");

        var rects = selected.Select(g => g.GetComponent<RectTransform>()).ToList();

        switch (alignment.ToLower())
        {
            case "left":
                float minX = rects.Min(r => r.anchoredPosition.x - r.rect.width * r.pivot.x);
                foreach (var r in rects)
                    r.anchoredPosition = new Vector2(minX + r.rect.width * r.pivot.x, r.anchoredPosition.y);
                break;
            case "right":
                float maxX = rects.Max(r => r.anchoredPosition.x + r.rect.width * (1 - r.pivot.x));
                foreach (var r in rects)
                    r.anchoredPosition = new Vector2(maxX - r.rect.width * (1 - r.pivot.x), r.anchoredPosition.y);
                break;
            case "center":
                float avgX = rects.Average(r => r.anchoredPosition.x);
                foreach (var r in rects)
                    r.anchoredPosition = new Vector2(avgX, r.anchoredPosition.y);
                break;
            case "top":
                float maxY = rects.Max(r => r.anchoredPosition.y + r.rect.height * (1 - r.pivot.y));
                foreach (var r in rects)
                    r.anchoredPosition = new Vector2(r.anchoredPosition.x, maxY - r.rect.height * (1 - r.pivot.y));
                break;
            case "bottom":
                float minY = rects.Min(r => r.anchoredPosition.y - r.rect.height * r.pivot.y);
                foreach (var r in rects)
                    r.anchoredPosition = new Vector2(r.anchoredPosition.x, minY + r.rect.height * r.pivot.y);
                break;
            case "middle":
                float avgY = rects.Average(r => r.anchoredPosition.y);
                foreach (var r in rects)
                    r.anchoredPosition = new Vector2(r.anchoredPosition.x, avgY);
                break;
            default:
                result.SetResult(new { error = $"Unknown alignment: {alignment}" });
                return;
        }

        result.SetResult(new { success = true, alignment, count = selected.Count });
    }
}
```
