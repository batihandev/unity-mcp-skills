# ui_layout_children

Arrange child UI elements using Vertical, Horizontal, or Grid layout groups.

**Signature:** `UILayoutChildren(name string = null, instanceId int = 0, path string = null, layoutType string = "Vertical", spacing float = 10, paddingLeft float = 0, paddingRight float = 0, paddingTop float = 0, paddingBottom float = 0, gridColumns int = 3, childForceExpandWidth bool = false, childForceExpandHeight bool = false)`

**Returns:** `{ success, parent, layoutType, childCount }`

**Notes:**
- Any existing layout group component is destroyed before adding the new one.
- A `ContentSizeFitter` is added automatically if not already present.
- For `Grid` layout, cell size is auto-calculated from the first child's `sizeDelta`.
- Returns an error for unknown `layoutType` values.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string layoutType = "Vertical";
        float spacing = 10f;
        float paddingLeft = 0f, paddingRight = 0f, paddingTop = 0f, paddingBottom = 0f;
        int gridColumns = 3;
        bool childForceExpandWidth = false, childForceExpandHeight = false;

        var (parentGo, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var rect = parentGo.GetComponent<RectTransform>();
        if (rect == null) { result.SetResult(new { error = "Parent has no RectTransform" }); return; }

        Undo.RecordObject(parentGo, "Add Layout");

        // Remove existing layout groups
        var existingV = parentGo.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        var existingH = parentGo.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        var existingG = parentGo.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (existingV) Undo.DestroyObjectImmediate(existingV);
        if (existingH) Undo.DestroyObjectImmediate(existingH);
        if (existingG) Undo.DestroyObjectImmediate(existingG);

        var padding = new RectOffset((int)paddingLeft, (int)paddingRight, (int)paddingTop, (int)paddingBottom);

        switch (layoutType.ToLower())
        {
            case "vertical":
                var vLayout = Undo.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(parentGo);
                vLayout.spacing = spacing;
                vLayout.padding = padding;
                vLayout.childForceExpandWidth = childForceExpandWidth;
                vLayout.childForceExpandHeight = childForceExpandHeight;
                break;
            case "horizontal":
                var hLayout = Undo.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>(parentGo);
                hLayout.spacing = spacing;
                hLayout.padding = padding;
                hLayout.childForceExpandWidth = childForceExpandWidth;
                hLayout.childForceExpandHeight = childForceExpandHeight;
                break;
            case "grid":
                var gLayout = Undo.AddComponent<UnityEngine.UI.GridLayoutGroup>(parentGo);
                gLayout.spacing = new Vector2(spacing, spacing);
                gLayout.padding = padding;
                gLayout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
                gLayout.constraintCount = gridColumns;
                // Auto-calculate cell size from first child
                if (rect.childCount > 0)
                {
                    var firstChild = rect.GetChild(0).GetComponent<RectTransform>();
                    if (firstChild != null)
                        gLayout.cellSize = firstChild.sizeDelta;
                }
                break;
            default:
                result.SetResult(new { error = $"Unknown layout type: {layoutType}" });
                return;
        }

        // Add ContentSizeFitter if not present
        if (parentGo.GetComponent<UnityEngine.UI.ContentSizeFitter>() == null)
        {
            var fitter = Undo.AddComponent<UnityEngine.UI.ContentSizeFitter>(parentGo);
            fitter.verticalFit = layoutType.ToLower() == "vertical"
                ? UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize
                : UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            fitter.horizontalFit = layoutType.ToLower() == "horizontal"
                ? UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize
                : UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        }

        result.SetResult(new { success = true, parent = parentGo.name, layoutType, childCount = rect.childCount });
    }
}
```
