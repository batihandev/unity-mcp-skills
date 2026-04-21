# ui_create_dropdown

Create a Dropdown UI element with a scrollable option list template.

**Signature:** `UICreateDropdown(name string = "Dropdown", parent string = null, options string = null, width float = 160, height float = 30)`

**Returns:** `{ success, name, instanceId, parent, optionCount }`

**Notes:**
- `options` is a comma-separated string (e.g. `"Option A,Option B,Option C"`).
- If `options` is empty or null, defaults to `["Option A", "Option B", "Option C"]`.
- Uses `TMP_Dropdown` when TMP is available; falls back to legacy `Dropdown`.
- Template child is deactivated after construction (standard Unity pattern).

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "Dropdown";
        string parent = null;
        string options = null;
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

        Component dropdownComp;
        bool usingTmpDropdown = IsTMPAvailable() && _tmpDropdownType != null;

        if (usingTmpDropdown)
            dropdownComp = go.AddComponent(_tmpDropdownType);
        else
            dropdownComp = go.AddComponent<Dropdown>();

        // Caption label
        var captionGo = new GameObject("Label");
        captionGo.transform.SetParent(go.transform, false);
        var captionRect = captionGo.AddComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(10, 0);
        captionRect.offsetMax = new Vector2(-25, 0);
        var captionText = AddTextComponent(captionGo, "", 14, Color.black);

        // Arrow
        var arrowGo = new GameObject("Arrow");
        arrowGo.transform.SetParent(go.transform, false);
        var arrowRect = arrowGo.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0);
        arrowRect.anchorMax = new Vector2(1, 1);
        arrowRect.pivot = new Vector2(1, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 0);
        var arrowImage = arrowGo.AddComponent<Image>();
        arrowImage.color = new Color(0.2f, 0.2f, 0.2f);

        // Template (dropdown list)
        var templateGo = new GameObject("Template");
        templateGo.transform.SetParent(go.transform, false);
        var templateRect = templateGo.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.sizeDelta = new Vector2(0, 150);
        var templateImage = templateGo.AddComponent<Image>();
        templateImage.color = Color.white;
        var scrollRect = templateGo.AddComponent<ScrollRect>();

        // Viewport
        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(templateGo.transform, false);
        var viewportRect = viewportGo.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportGo.AddComponent<RectMask2D>();
        var viewportImage = viewportGo.AddComponent<Image>();
        viewportImage.color = new Color(1, 1, 1, 0);

        // Content
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 28);

        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Item
        var itemGo = new GameObject("Item");
        itemGo.transform.SetParent(contentGo.transform, false);
        var itemRect = itemGo.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.sizeDelta = new Vector2(0, 28);
        var itemToggle = itemGo.AddComponent<Toggle>();

        // Item background
        var itemBgGo = new GameObject("Item Background");
        itemBgGo.transform.SetParent(itemGo.transform, false);
        var itemBgRect = itemBgGo.AddComponent<RectTransform>();
        itemBgRect.anchorMin = Vector2.zero;
        itemBgRect.anchorMax = Vector2.one;
        itemBgRect.sizeDelta = Vector2.zero;
        var itemBgImage = itemBgGo.AddComponent<Image>();
        itemBgImage.color = new Color(0.96f, 0.96f, 0.96f);

        // Item checkmark
        var checkGo = new GameObject("Item Checkmark");
        checkGo.transform.SetParent(itemGo.transform, false);
        var checkRect = checkGo.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0, 0.5f);
        checkRect.anchorMax = new Vector2(0, 0.5f);
        checkRect.sizeDelta = new Vector2(20, 20);
        checkRect.anchoredPosition = new Vector2(10, 0);
        var checkImage = checkGo.AddComponent<Image>();
        checkImage.color = new Color(0.3f, 0.6f, 1f);

        itemToggle.targetGraphic = itemBgImage;
        itemToggle.graphic = checkImage;
        itemToggle.isOn = true;

        // Item label
        var itemLabelGo = new GameObject("Item Label");
        itemLabelGo.transform.SetParent(itemGo.transform, false);
        var itemLabelRect = itemLabelGo.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(25, 0);
        itemLabelRect.offsetMax = Vector2.zero;
        var itemLabelText = AddTextComponent(itemLabelGo, "Option", 14, Color.black);

        // Set dropdown references via reflection (TMP) or direct (Legacy)
        if (usingTmpDropdown)
        {
            _tmpDropdownType.GetProperty("captionText")?.SetValue(dropdownComp, captionText);
            _tmpDropdownType.GetProperty("itemText")?.SetValue(dropdownComp, itemLabelText);
            _tmpDropdownType.GetProperty("template")?.SetValue(dropdownComp, templateRect);
        }
        else
        {
            var dd = (Dropdown)dropdownComp;
            dd.captionText = captionText as Text;
            dd.itemText = itemLabelText as Text;
            dd.template = templateRect;
        }

        templateGo.SetActive(false);

        // Add options
        var optionList = new List<string>();
        if (!string.IsNullOrEmpty(options))
        {
            foreach (var opt in options.Split(','))
            {
                var trimmed = opt.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    optionList.Add(trimmed);
            }
        }
        if (optionList.Count == 0)
            optionList.AddRange(new[] { "Option A", "Option B", "Option C" });

        if (usingTmpDropdown)
        {
            var addMethod = _tmpDropdownType.GetMethod("AddOptions", new[] { typeof(List<string>) });
            addMethod?.Invoke(dropdownComp, new object[] { optionList });
        }
        else
        {
            ((Dropdown)dropdownComp).AddOptions(optionList);
        }

        Undo.RegisterCreatedObjectUndo(go, "Create Dropdown");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new { success = true, name = go.name, instanceId = go.GetInstanceID(), parent = parentGo.name, optionCount = optionList.Count });
    }
}
```
