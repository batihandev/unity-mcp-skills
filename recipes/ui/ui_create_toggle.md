# ui_create_toggle

Create a Toggle (checkbox) UI element with background, checkmark, and label.

**Signature:** `UICreateToggle(name string = "Toggle", parent string = null, label string = "Toggle", isOn bool = false)`

**Returns:** `{ success, name, instanceId, parent, label, isOn }`

**Notes:**
- Background is white 20x20, pinned to top-left of the toggle root.
- Checkmark fill color is blue `(0.3, 0.6, 1)`.
- Label text uses TMP if available, otherwise legacy `Text`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

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
        var bgImage = bgGo.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = Color.white;

        // Checkmark
        var checkGo = new GameObject("Checkmark");
        checkGo.transform.SetParent(bgGo.transform, false);
        var checkRect = checkGo.AddComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.sizeDelta = Vector2.zero;
        var checkImage = checkGo.AddComponent<UnityEngine.UI.Image>();
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
