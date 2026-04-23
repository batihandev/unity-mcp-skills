# ui_add_mask

Add a Mask or RectMask2D component to a UI element for content clipping.

**Signature:** `UIAddMask(name string = null, instanceId int = 0, path string = null, maskType string = "RectMask2D", showMaskGraphic bool = true)`

**Returns:** `{ success, name, maskType, showMaskGraphic }`

**Notes:**
- `maskType` values: `Mask` or `RectMask2D`.
- `Mask` requires an `Image` component — one is automatically added if missing.
- `RectMask2D` is generally preferred for rectangular clipping (no stencil buffer cost).
- `showMaskGraphic` only applies to the `Mask` type.

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
        string maskType = "RectMask2D";
        bool showMaskGraphic = true;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        WorkflowManager.SnapshotObject(go);
        Undo.RecordObject(go, "Add Mask");

        string applied;
        if (maskType.Equals("Mask", StringComparison.OrdinalIgnoreCase))
        {
            // Mask requires an Image component
            if (go.GetComponent<UnityEngine.UI.Image>() == null)
                Undo.AddComponent<UnityEngine.UI.Image>(go);
            var mask = go.GetComponent<Mask>() ?? Undo.AddComponent<Mask>(go);
            mask.showMaskGraphic = showMaskGraphic;
            applied = "Mask";
        }
        else
        {
            var rectMask = go.GetComponent<RectMask2D>() ?? Undo.AddComponent<RectMask2D>(go);
            applied = "RectMask2D";
        }

        result.SetResult(new { success = true, name = go.name, maskType = applied, showMaskGraphic });
    }
}
```
