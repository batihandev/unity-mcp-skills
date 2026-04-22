# ui_distribute_selected

Distribute the currently selected UI elements evenly along a horizontal or vertical axis.

**Signature:** `UIDistributeSelected(direction string = "Horizontal")`

**Returns:** `{ success, direction, count }`

**Notes:**
- Requires at least 3 selected GameObjects with a `RectTransform`.
- Elements are sorted by position along the chosen axis before distribution.
- The first and last elements keep their positions; intermediate elements are evenly spaced.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string direction = "Horizontal";

        var selected = Selection.gameObjects
            .Where(g => g.GetComponent<RectTransform>() != null)
            .OrderBy(g => direction.ToLower() == "horizontal"
                ? g.GetComponent<RectTransform>().anchoredPosition.x
                : g.GetComponent<RectTransform>().anchoredPosition.y)
            .ToList();

        if (selected.Count < 3)
        {
            result.SetResult(new { error = "Select at least 3 UI elements to distribute" });
            return;
        }

        Undo.RecordObjects(selected.Select(g => g.GetComponent<RectTransform>()).Cast<UnityEngine.Object>().ToArray(), "Distribute UI");

        var rects = selected.Select(g => g.GetComponent<RectTransform>()).ToList();

        if (direction.ToLower() == "horizontal")
        {
            float minX = rects.First().anchoredPosition.x;
            float maxX = rects.Last().anchoredPosition.x;
            float step = (maxX - minX) / (rects.Count - 1);

            for (int i = 0; i < rects.Count; i++)
                rects[i].anchoredPosition = new Vector2(minX + step * i, rects[i].anchoredPosition.y);
        }
        else
        {
            float minY = rects.First().anchoredPosition.y;
            float maxY = rects.Last().anchoredPosition.y;
            float step = (maxY - minY) / (rects.Count - 1);

            for (int i = 0; i < rects.Count; i++)
                rects[i].anchoredPosition = new Vector2(rects[i].anchoredPosition.x, minY + step * i);
        }

        result.SetResult(new { success = true, direction, count = selected.Count });
    }
}
```
