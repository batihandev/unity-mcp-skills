# ui_create_toggle

Create a Toggle (checkbox) UI element with background, checkmark, and label.

**Signature:** `UICreateToggle(name string = "Toggle", parent string = null, label string = "Toggle", isOn bool = false)`

**Returns:** `{ success, name, instanceId, parent, label, isOn }`

**Notes:**
- Background is white 20x20, pinned to top-left of the toggle root.
- Checkmark fill color is blue `(0.3, 0.6, 1)`.
- Label text uses TMP if available, otherwise legacy `Text`.

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
        string name = "Toggle";
        string parent = null;
        string label = "Toggle";
        bool isOn = false;

        var parentGo = FindOrCreateCanvas(parent);
        if (parentGo == null)
        {
            result.SetResult(new { error = "Parent not found and could not create Canvas" });
            return;
        }

        var go = new GameObject(name);
        go.transform.SetParent(parentGo.transform, false);

        var rectTransform = go.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(160, 20);

        var toggle = go.AddComponent<Toggle>();
        toggle.isOn = isOn;

        // Background
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(go.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(0, 1);
        bgRect.pivot = new Vector2(0, 1);
        bgRect.sizeDelta = new Vector2(20, 20);
        var bgImage = bgGo.AddComponent<Image>();
        bgImage.color = Color.white;

        // Checkmark
        var checkGo = new GameObject("Checkmark");
        checkGo.transform.SetParent(bgGo.transform, false);
        var checkRect = checkGo.AddComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.sizeDelta = Vector2.zero;
        var checkImage = checkGo.AddComponent<Image>();
        checkImage.color = new Color(0.3f, 0.6f, 1f);

        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;

        // Label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(25, 0);
        labelRect.offsetMax = Vector2.zero;

        AddTextComponent(labelGo, label, 14, Color.black);

        Undo.RegisterCreatedObjectUndo(go, "Create Toggle");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new { success = true, name = go.name, instanceId = go.GetInstanceID(), parent = parentGo.name, label, isOn });
    }
}
```
