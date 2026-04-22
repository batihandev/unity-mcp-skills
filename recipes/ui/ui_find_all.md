# ui_find_all

Find all UI elements in the scene, optionally filtered by UI type.

**Signature:** `UIFindAll(uiType string = null, limit int = 50)`

**Returns:** `{ count, elements }`

**Notes:**
- `uiType` filter values (case-insensitive): `Canvas`, `Button`, `Slider`, `Toggle`, `InputField`, `Text`, `Image`, `RawImage`, `RectTransform`.
- Each element entry contains `name`, `instanceId`, `path`, `uiType`, and `active`.
- Search walks all Canvas trees in the scene including inactive children.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string uiType = null;
        int limit = 50;

        var canvases = FindHelper.FindAll<Canvas>();
        var results = new System.Collections.Generic.List<object>();

        foreach (var canvas in canvases)
        {
            var elements = canvas.GetComponentsInChildren<RectTransform>(true);
            foreach (var element in elements)
            {
                if (results.Count >= limit) break;

                var type = GetUIType(element.gameObject);
                if (!string.IsNullOrEmpty(uiType) && !string.Equals(type, uiType, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                results.Add(new
                {
                    name = element.name,
                    instanceId = element.gameObject.GetInstanceID(),
                    path = GameObjectFinder.GetCachedPath(element.gameObject),
                    uiType = type,
                    active = element.gameObject.activeInHierarchy
                });
            }
        }

        result.SetResult(new { count = results.Count, elements = results });
    }

    private static bool _tmpChecked;
    private static bool _tmpAvailable;
    private static System.Type _tmpTextType;
    private static System.Type _tmpInputFieldType;

    private static bool IsTMPAvailable()
    {
        if (!_tmpChecked)
        {
            _tmpChecked = true;
            _tmpTextType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            _tmpInputFieldType = System.Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            _tmpAvailable = _tmpTextType != null;
        }
        return _tmpAvailable;
    }

    private static string GetUIType(GameObject go)
    {
        if (go.GetComponent<Canvas>()) return "Canvas";
        if (go.GetComponent<Button>()) return "Button";
        if (go.GetComponent<Slider>()) return "Slider";
        if (go.GetComponent<Toggle>()) return "Toggle";
        if (IsTMPAvailable())
        {
            if (_tmpInputFieldType != null && go.GetComponent(_tmpInputFieldType) != null) return "InputField";
            if (_tmpTextType != null && go.GetComponent(_tmpTextType) != null) return "Text";
        }
        if (go.GetComponent<InputField>()) return "InputField";
        if (go.GetComponent<Text>()) return "Text";
        if (go.GetComponent<UnityEngine.UI.Image>()) return "Image";
        if (go.GetComponent<RawImage>()) return "RawImage";
        if (go.GetComponent<RectTransform>()) return "RectTransform";
        return "Unknown";
    }
}
```
