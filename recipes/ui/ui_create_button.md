# ui_create_button

Create a Button UI element with a text child label.

**Signature:** `UICreateButton(name string = "Button", parent string = null, text string = "Button", width float = 160, height float = 30)`

**Returns:** `{ success, name, instanceId, parent, text }`

**Notes:**
- Text child uses TMP (`TextMeshProUGUI`) if available, otherwise legacy `Text`.
- Use `ui_set_text` to change button label text after creation.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Button";
        string parent = null;
        string text = "Button";
        float width = 160f, height = 30f;

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

        var image = go.AddComponent<Image>();
        image.color = Color.white;

        var button = go.AddComponent<Button>();

        // Add text child
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        AddTextComponent(textGo, text, 14, Color.black, TextAnchor.MiddleCenter);

        Undo.RegisterCreatedObjectUndo(go, "Create Button");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new { success = true, name = go.name, instanceId = go.GetInstanceID(), parent = parentGo.name, text });
    }
}
```
