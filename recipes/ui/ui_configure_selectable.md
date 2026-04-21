# ui_configure_selectable

Configure Selectable component properties: transition mode, navigation, and color block.

**Signature:** `UIConfigureSelectable(name string = null, instanceId int = 0, path string = null, transition string = null, interactable bool? = null, navigationMode string = null, normalR float? = null, normalG float? = null, normalB float? = null, highlightedR float? = null, highlightedG float? = null, highlightedB float? = null, pressedR float? = null, pressedG float? = null, pressedB float? = null, disabledR float? = null, disabledG float? = null, disabledB float? = null, colorMultiplier float? = null, fadeDuration float? = null)`

**Returns:** `{ success, name, transition, interactable, navigationMode }`

**Notes:**
- Works on any `Selectable` subclass: `Button`, `Toggle`, `Slider`, `Dropdown`, `InputField`.
- `transition` values: `None`, `ColorTint`, `SpriteSwap`, `Animation`.
- `navigationMode` values: `None`, `Horizontal`, `Vertical`, `Automatic`, `Explicit`.
- Color channels are applied independently; omit any channel to keep its current value.

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
        string transition = null;
        bool? interactable = null;
        string navigationMode = null;
        float? normalR = null, normalG = null, normalB = null;
        float? highlightedR = null, highlightedG = null, highlightedB = null;
        float? pressedR = null, pressedG = null, pressedB = null;
        float? disabledR = null, disabledG = null, disabledB = null;
        float? colorMultiplier = null, fadeDuration = null;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var selectable = go.GetComponent<Selectable>();
        if (selectable == null) { result.SetResult(new { error = "No Selectable component found (Button, Toggle, Slider, etc.)" }); return; }

        WorkflowManager.SnapshotObject(selectable);
        Undo.RecordObject(selectable, "Configure Selectable");

        if (interactable.HasValue)
            selectable.interactable = interactable.Value;

        if (!string.IsNullOrEmpty(transition) && Enum.TryParse<Selectable.Transition>(transition, true, out var trans))
            selectable.transition = trans;

        if (!string.IsNullOrEmpty(navigationMode))
        {
            if (Enum.TryParse<Navigation.Mode>(navigationMode, true, out var navMode))
            {
                var nav = selectable.navigation;
                nav.mode = navMode;
                selectable.navigation = nav;
            }
        }

        // Update colors if any color param is provided
        if (normalR.HasValue || highlightedR.HasValue || pressedR.HasValue || disabledR.HasValue ||
            colorMultiplier.HasValue || fadeDuration.HasValue)
        {
            var colors = selectable.colors;
            if (normalR.HasValue || normalG.HasValue || normalB.HasValue)
                colors.normalColor = new Color(normalR ?? colors.normalColor.r, normalG ?? colors.normalColor.g, normalB ?? colors.normalColor.b);
            if (highlightedR.HasValue || highlightedG.HasValue || highlightedB.HasValue)
                colors.highlightedColor = new Color(highlightedR ?? colors.highlightedColor.r, highlightedG ?? colors.highlightedColor.g, highlightedB ?? colors.highlightedColor.b);
            if (pressedR.HasValue || pressedG.HasValue || pressedB.HasValue)
                colors.pressedColor = new Color(pressedR ?? colors.pressedColor.r, pressedG ?? colors.pressedColor.g, pressedB ?? colors.pressedColor.b);
            if (disabledR.HasValue || disabledG.HasValue || disabledB.HasValue)
                colors.disabledColor = new Color(disabledR ?? colors.disabledColor.r, disabledG ?? colors.disabledColor.g, disabledB ?? colors.disabledColor.b);
            if (colorMultiplier.HasValue)
                colors.colorMultiplier = colorMultiplier.Value;
            if (fadeDuration.HasValue)
                colors.fadeDuration = fadeDuration.Value;
            selectable.colors = colors;
        }

        result.SetResult(new
        {
            success = true, name = go.name,
            transition = selectable.transition.ToString(),
            interactable = selectable.interactable,
            navigationMode = selectable.navigation.mode.ToString()
        });
    }
}
```
