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
}
```
