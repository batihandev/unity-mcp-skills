# ui_add_layout_element

Add or configure a LayoutElement component on a UI element for layout constraint hints.

**Signature:** `UIAddLayoutElement(name string = null, instanceId int = 0, path string = null, minWidth float? = null, minHeight float? = null, preferredWidth float? = null, preferredHeight float? = null, flexibleWidth float? = null, flexibleHeight float? = null, ignoreLayout bool? = null, layoutPriority int? = null)`

**Returns:** `{ success, name, minWidth, minHeight, preferredWidth, preferredHeight, flexibleWidth, flexibleHeight, ignoreLayout }`

**Notes:**
- Adds a new `LayoutElement` if one is not already present; otherwise modifies the existing one.
- All size parameters are optional; only provided values are set.
- `ignoreLayout = true` removes this element from layout group calculations entirely.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

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
        float? minWidth = null, minHeight = null;
        float? preferredWidth = null, preferredHeight = null;
        float? flexibleWidth = null, flexibleHeight = null;
        bool? ignoreLayout = null;
        int? layoutPriority = null;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var layout = go.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(go);
        WorkflowManager.SnapshotObject(layout);
        Undo.RecordObject(layout, "Set LayoutElement");

        if (minWidth.HasValue) layout.minWidth = minWidth.Value;
        if (minHeight.HasValue) layout.minHeight = minHeight.Value;
        if (preferredWidth.HasValue) layout.preferredWidth = preferredWidth.Value;
        if (preferredHeight.HasValue) layout.preferredHeight = preferredHeight.Value;
        if (flexibleWidth.HasValue) layout.flexibleWidth = flexibleWidth.Value;
        if (flexibleHeight.HasValue) layout.flexibleHeight = flexibleHeight.Value;
        if (ignoreLayout.HasValue) layout.ignoreLayout = ignoreLayout.Value;
        if (layoutPriority.HasValue) layout.layoutPriority = layoutPriority.Value;

        result.SetResult(new
        {
            success = true, name = go.name,
            minWidth = layout.minWidth, minHeight = layout.minHeight,
            preferredWidth = layout.preferredWidth, preferredHeight = layout.preferredHeight,
            flexibleWidth = layout.flexibleWidth, flexibleHeight = layout.flexibleHeight,
            ignoreLayout = layout.ignoreLayout
        });
    }
}
```
