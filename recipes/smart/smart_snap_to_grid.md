# smart_snap_to_grid

Snap all selected objects to a grid.

**Signature:** `SmartSnapToGrid(float gridSize = 1f)`

**Returns:** `{ success, snapped, gridSize }`

**Notes:**
- Rounds each position component to the nearest multiple of `gridSize`
- Requires objects selected in Hierarchy first
- Undoable

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float gridSize = 1f;

        var selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            result.SetResult(new { error = "No objects selected" });
            return;
        }

        foreach (var go in selected)
        {
            Undo.RecordObject(go.transform, "Snap To Grid");
            var p = go.transform.position;
            go.transform.position = new Vector3(
                Mathf.Round(p.x / gridSize) * gridSize,
                Mathf.Round(p.y / gridSize) * gridSize,
                Mathf.Round(p.z / gridSize) * gridSize);
        }

        result.SetResult(new { success = true, snapped = selected.Length, gridSize });
    }
}
```
