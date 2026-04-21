# ui_create_slider

Create a Slider UI element with background, fill area, and handle.

**Signature:** `UICreateSlider(name string = "Slider", parent string = null, minValue float = 0, maxValue float = 1, value float = 0.5, width float = 160, height float = 20)`

**Returns:** `{ success, name, instanceId, parent, minValue, maxValue, value }`

**Notes:**
- Background is grey `(0.8, 0.8, 0.8)`, fill is blue `(0.3, 0.6, 1)`, handle is white.
- Fill area and handle area both get a 10-unit margin on each side via `sizeDelta`.

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
        string name = "Slider";
        string parent = null;
        float minValue = 0f, maxValue = 1f, value = 0.5f;
        float width = 160f, height = 20f;

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

        var slider = go.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;

        // Background
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(go.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        var bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0.8f, 0.8f, 0.8f);

        // Fill Area
        var fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(go.transform, false);
        var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.sizeDelta = new Vector2(-20, 0);

        // Fill
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(10, 0);
        var fillImage = fillGo.AddComponent<Image>();
        fillImage.color = new Color(0.3f, 0.6f, 1f);

        slider.fillRect = fillRect;

        // Handle
        var handleAreaGo = new GameObject("Handle Slide Area");
        handleAreaGo.transform.SetParent(go.transform, false);
        var handleAreaRect = handleAreaGo.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-20, 0);

        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(handleAreaGo.transform, false);
        var handleRect = handleGo.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);
        var handleImage = handleGo.AddComponent<Image>();
        handleImage.color = Color.white;

        slider.handleRect = handleRect;

        Undo.RegisterCreatedObjectUndo(go, "Create Slider");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new { success = true, name = go.name, instanceId = go.GetInstanceID(), parent = parentGo.name, minValue, maxValue, value });
    }
}
```
