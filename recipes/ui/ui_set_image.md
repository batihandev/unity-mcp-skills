# ui_set_image

Configure Image component properties: type, fill, sprite, and pixel density.

**Signature:** `UISetImage(name string = null, instanceId int = 0, path string = null, type string = null, fillMethod string = null, fillAmount float? = null, fillClockwise bool? = null, fillOrigin int? = null, preserveAspect bool? = null, spritePath string = null, pixelsPerUnitMultiplier float? = null)`

**Returns:** `{ success, name, type, fillMethod, fillAmount, preserveAspect }`

**Notes:**
- All properties are optional; only provided values are applied.
- `type` values: `Simple`, `Sliced`, `Tiled`, `Filled`.
- `fillMethod` values: `Horizontal`, `Vertical`, `Radial90`, `Radial180`, `Radial360`.
- Returns an error if the sprite at `spritePath` is not found.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
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

        var image = go.GetComponent<Image>();
        if (image == null) { result.SetResult(new { error = "No Image component found" }); return; }

        WorkflowManager.SnapshotObject(image);
        Undo.RecordObject(image, "Set Image");

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<Image.Type>(type, true, out var imgType))
            image.type = imgType;
        if (!string.IsNullOrEmpty(fillMethod) && Enum.TryParse<Image.FillMethod>(fillMethod, true, out var fm))
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
