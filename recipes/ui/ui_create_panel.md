# ui_create_panel

Create a Panel UI element (full-stretch Image container) under a Canvas.

**Signature:** `UICreatePanel(name string = "Panel", parent string = null, r float = 1, g float = 1, b float = 1, a float = 0.5)`

**Returns:** `{ success, name, instanceId, parent }`

**Notes:**
- If `parent` is not found, the skill auto-creates or reuses the first Canvas in the scene.
- The panel stretches to fill its parent by default (`anchorMin = 0,0` / `anchorMax = 1,1`).

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Panel";
        string parent = null;
        float r = 1f, g = 1f, b = 1f, a = 0.5f;

        var parentGo = FindOrCreateCanvas(parent);
        if (parentGo == null)
        {
            result.SetResult(new { error = "Parent not found and could not create Canvas" });
            return;
        }

        var go = new GameObject(name);
        go.transform.SetParent(parentGo.transform, false);

        var rectTransform = go.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = new Color(r, g, b, a);

        Undo.RegisterCreatedObjectUndo(go, "Create Panel");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new { success = true, name = go.name, instanceId = go.GetInstanceID(), parent = parentGo.name });
    }
}
```
