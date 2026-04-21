# ui_create_inputfield

Create an InputField UI element with placeholder and text children.

**Signature:** `UICreateInputField(name string = "InputField", parent string = null, placeholder string = "Enter text...", width float = 200, height float = 30)`

**Returns:** `{ success, name, instanceId, parent, placeholder, usingTMP }`

**Notes:**
- Uses `TMP_InputField` when TMP is present; falls back to legacy `InputField`.
- TMP path creates a "Text Area" viewport with `RectMask2D`; legacy path creates flat children directly on the root.
- Check `usingTMP` in the response before any component-specific post-processing.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "InputField";
        string parent = null;
        string placeholder = "Enter text...";
        float width = 200f, height = 30f;

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

        if (IsTMPAvailable())
        {
            // Use TMP InputField
            var inputField = go.AddComponent(_tmpInputFieldType);

            // Create text area
            var textAreaGo = new GameObject("Text Area");
            textAreaGo.transform.SetParent(go.transform, false);
            var textAreaRect = textAreaGo.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -7);
            textAreaGo.AddComponent<RectMask2D>();

            // Placeholder
            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(textAreaGo.transform, false);
            var placeholderRect = placeholderGo.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            var placeholderComp = AddTextComponent(placeholderGo, placeholder, 14, new Color(0.5f, 0.5f, 0.5f));
            // Set italic style
            var fontStyleType = Type.GetType("TMPro.FontStyles, Unity.TextMeshPro");
            if (fontStyleType != null)
                _tmpTextType.GetProperty("fontStyle")?.SetValue(placeholderComp, Enum.Parse(fontStyleType, "Italic"));

            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(textAreaGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            var textComp = AddTextComponent(textGo, "", 14, Color.black);

            // Set TMP_InputField properties
            _tmpInputFieldType.GetProperty("textViewport")?.SetValue(inputField, textAreaRect);
            _tmpInputFieldType.GetProperty("textComponent")?.SetValue(inputField, textComp);
            _tmpInputFieldType.GetProperty("placeholder")?.SetValue(inputField, placeholderComp);
        }
        else
        {
            // Use Legacy InputField
            var inputField = go.AddComponent<InputField>();

            // Placeholder
            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(go.transform, false);
            var placeholderRect = placeholderGo.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 6);
            placeholderRect.offsetMax = new Vector2(-10, -7);
            var placeholderText = placeholderGo.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f);
            placeholderText.fontStyle = FontStyle.Italic;
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (placeholderText.font == null)
                placeholderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 6);
            textRect.offsetMax = new Vector2(-10, -7);
            var text = textGo.AddComponent<Text>();
            text.color = Color.black;
            text.supportRichText = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            inputField.textComponent = text;
            inputField.placeholder = placeholderText;
        }

        Undo.RegisterCreatedObjectUndo(go, "Create InputField");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new { success = true, name = go.name, instanceId = go.GetInstanceID(), parent = parentGo.name, placeholder, usingTMP = IsTMPAvailable() });
    }
}
```
