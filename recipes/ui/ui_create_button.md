# ui_create_button

Create a Button UI element with a text child label.

**Signature:** `UICreateButton(name string = "Button", parent string = null, text string = "Button", width float = 160, height float = 30)`

**Returns:** `{ success, name, instanceId, parent, text }`

**Notes:**
- Text child uses TMP (`TextMeshProUGUI`) if available, otherwise legacy `Text`.
- Use `ui_set_text` to change button label text after creation.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

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

        var image = go.AddComponent<UnityEngine.UI.Image>();
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

    private static bool _tmpChecked;
    private static bool _tmpAvailable;
    private static System.Type _tmpTextType;
    private static System.Type _tmpInputFieldType;
    private static System.Type _tmpDropdownType;

    private static bool IsTMPAvailable()
    {
        if (!_tmpChecked)
        {
            _tmpChecked = true;
            _tmpTextType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            _tmpInputFieldType = System.Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            _tmpDropdownType = System.Type.GetType("TMPro.TMP_Dropdown, Unity.TextMeshPro");
            _tmpAvailable = _tmpTextType != null;
        }
        return _tmpAvailable;
    }

    private static Component AddTextComponent(GameObject go, string text, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        if (IsTMPAvailable())
        {
            var tmp = go.AddComponent(_tmpTextType);
            _tmpTextType.GetProperty("text")?.SetValue(tmp, text);
            _tmpTextType.GetProperty("fontSize")?.SetValue(tmp, (float)fontSize);
            _tmpTextType.GetProperty("color")?.SetValue(tmp, color);
            var alignOptionsType = System.Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
            if (alignOptionsType != null)
            {
                object tmpAlignment = alignment switch
                {
                    TextAnchor.UpperLeft => System.Enum.Parse(alignOptionsType, "TopLeft"),
                    TextAnchor.UpperCenter => System.Enum.Parse(alignOptionsType, "Top"),
                    TextAnchor.UpperRight => System.Enum.Parse(alignOptionsType, "TopRight"),
                    TextAnchor.MiddleLeft => System.Enum.Parse(alignOptionsType, "Left"),
                    TextAnchor.MiddleCenter => System.Enum.Parse(alignOptionsType, "Center"),
                    TextAnchor.MiddleRight => System.Enum.Parse(alignOptionsType, "Right"),
                    TextAnchor.LowerLeft => System.Enum.Parse(alignOptionsType, "BottomLeft"),
                    TextAnchor.LowerCenter => System.Enum.Parse(alignOptionsType, "Bottom"),
                    TextAnchor.LowerRight => System.Enum.Parse(alignOptionsType, "BottomRight"),
                    _ => System.Enum.Parse(alignOptionsType, "Center")
                };
                _tmpTextType.GetProperty("alignment")?.SetValue(tmp, tmpAlignment);
            }
            return tmp;
        }
        else
        {
            var textComp = go.AddComponent<Text>();
            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.color = color;
            textComp.alignment = alignment;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (textComp.font == null)
                textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return textComp;
        }
    }

    private static GameObject FindOrCreateCanvas(string parentName)
    {
        if (!string.IsNullOrEmpty(parentName))
        {
            var p = GameObject.Find(parentName);
            if (p != null) return p;
        }
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null) return canvas.gameObject;
        var go = new GameObject("Canvas");
        var canvasComp = go.AddComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);
        return go;
    }
}
```
