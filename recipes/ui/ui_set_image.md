# ui_set_image

Configure Image component properties: type, fill, sprite, and pixel density.

**Signature:** `UISetImage(name string = null, instanceId int = 0, path string = null, type string = null, fillMethod string = null, fillAmount float? = null, fillClockwise bool? = null, fillOrigin int? = null, preserveAspect bool? = null, spritePath string = null, pixelsPerUnitMultiplier float? = null)`

**Returns:** `{ success, name, type, fillMethod, fillAmount, preserveAspect }`

**Notes:**
- All properties are optional; only provided values are applied.
- `type` values: `Simple`, `Sliced`, `Tiled`, `Filled`.
- `fillMethod` values: `Horizontal`, `Vertical`, `Radial90`, `Radial180`, `Radial360`.
- Returns an error if the sprite at `spritePath` is not found.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        string type = null;
        string fillMethod = null;
        float? fillAmount = null;
        bool? fillClockwise = null;
        int? fillOrigin = null;
        bool? preserveAspect = null;
        string spritePath = null;
        float? pixelsPerUnitMultiplier = null;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var image = go.GetComponent<UnityEngine.UI.Image>();
        if (image == null) { result.SetResult(new { error = "No Image component found" }); return; }

        WorkflowManager.SnapshotObject(image);
        Undo.RecordObject(image, "Set Image");

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<UnityEngine.UI.Image.Type>(type, true, out var imgType))
            image.type = imgType;
        if (!string.IsNullOrEmpty(fillMethod) && Enum.TryParse<UnityEngine.UI.Image.FillMethod>(fillMethod, true, out var fm))
            image.fillMethod = fm;
        if (fillAmount.HasValue)
            image.fillAmount = fillAmount.Value;
        if (fillClockwise.HasValue)
            image.fillClockwise = fillClockwise.Value;
        if (fillOrigin.HasValue)
            image.fillOrigin = fillOrigin.Value;
        if (preserveAspect.HasValue)
            image.preserveAspect = preserveAspect.Value;
        if (pixelsPerUnitMultiplier.HasValue)
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier.Value;

        if (!string.IsNullOrEmpty(spritePath))
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
                image.sprite = sprite;
            else
            {
                result.SetResult(new { error = $"Sprite not found: {spritePath}" });
                return;
            }
        }

        result.SetResult(new
        {
            success = true, name = go.name,
            type = image.type.ToString(),
            fillMethod = image.fillMethod.ToString(),
            fillAmount = image.fillAmount,
            preserveAspect = image.preserveAspect
        });
    }
}
```
