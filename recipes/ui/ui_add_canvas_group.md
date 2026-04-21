# ui_add_canvas_group

Add or configure a CanvasGroup on a UI element to control alpha, interactability, and raycasting.

**Signature:** `UIAddCanvasGroup(name string = null, instanceId int = 0, path string = null, alpha float? = null, interactable bool? = null, blocksRaycasts bool? = null, ignoreParentGroups bool? = null)`

**Returns:** `{ success, name, alpha, interactable, blocksRaycasts, ignoreParentGroups }`

**Notes:**
- Creates a new `CanvasGroup` if one is not already present; otherwise modifies the existing one.
- Useful for fade animations (set `alpha`) and disabling entire UI sections (`interactable = false`).

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;
        float? alpha = null;
        bool? interactable = null, blocksRaycasts = null, ignoreParentGroups = null;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var group = go.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = go.AddComponent<CanvasGroup>();
            Undo.RegisterCreatedObjectUndo(group, "Add CanvasGroup");
        }
        WorkflowManager.SnapshotObject(group);
        Undo.RecordObject(group, "Set CanvasGroup");

        if (alpha.HasValue) group.alpha = alpha.Value;
        if (interactable.HasValue) group.interactable = interactable.Value;
        if (blocksRaycasts.HasValue) group.blocksRaycasts = blocksRaycasts.Value;
        if (ignoreParentGroups.HasValue) group.ignoreParentGroups = ignoreParentGroups.Value;

        result.SetResult(new
        {
            success = true, name = go.name,
            alpha = group.alpha, interactable = group.interactable,
            blocksRaycasts = group.blocksRaycasts, ignoreParentGroups = group.ignoreParentGroups
        });
    }
}
```
