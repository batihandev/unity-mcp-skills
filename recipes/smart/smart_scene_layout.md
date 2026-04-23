# smart_scene_layout

Organize selected objects into a spatial layout (Linear, Grid, Circle, Arc). Requires objects selected in Hierarchy first.

**Signature:** `SmartSceneLayout(string layoutType = "Linear", string axis = "X", float spacing = 2.0f, int columns = 3, float arcAngle = 180f, bool lookAtCenter = false)`

**Returns:** `{ success, layout, count, spacing }`

**Notes:**
- Select objects in Hierarchy before calling.
- `layoutType` values: `Linear`, `Grid`, `Circle`, `Arc` (case-insensitive).
- `axis` values: `X`, `Y`, `Z`, `-X`, `-Y`, `-Z` — used for Linear layout; ignored for Circle/Arc.
- `spacing` is the distance between items for Linear/Grid, or the radius for Circle/Arc.
- `columns` applies only to Grid layout.
- `arcAngle` applies only to Arc layout (total arc in degrees).
- `lookAtCenter` rotates each object to face the center; applies to Circle and Arc only.
- Supports undo and workflow snapshots.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string layoutType = "Grid";   // Linear, Grid, Circle, Arc
        string axis = "X";            // X, Y, Z, -X, -Y, -Z (Linear only)
        float spacing = 2.0f;         // spacing or radius
        int columns = 3;              // Grid only
        float arcAngle = 180f;        // Arc only (degrees)
        bool lookAtCenter = false;    // Circle/Arc only

        var selected = Selection.gameObjects.OrderBy(g => g.transform.GetSiblingIndex()).ToList();
        if (selected.Count == 0) 
            { result.SetResult(new { success = false, error = "No GameObjects selected. Select objects in Hierarchy first." }); return; }

        // Workflow 鏀寔
        foreach (var go in selected)
            WorkflowManager.SnapshotObject(go.transform);

        Undo.RecordObjects(selected.Select(g => g.transform).ToArray(), "Smart Layout");

        var startPos = selected[0].transform.position;
        Vector3 axisVec = ParseAxis(axis);

        for (int i = 0; i < selected.Count; i++)
        {
            Vector3 newPos = startPos;
    
            switch (layoutType.ToLower())
            {
                case "linear":
                    newPos = startPos + axisVec * (i * spacing);
                    break;

                case "grid":
                    int row = i / columns;
                    int col = i % columns;
                    // Grid on XZ plane by default
                    newPos = startPos + new Vector3(col * spacing, 0, -row * spacing); 
                    break;

                case "circle":
                    float angle = i * (360f / selected.Count);
                    Vector3 offset = Quaternion.Euler(0, angle, 0) * (Vector3.forward * spacing);
                    newPos = startPos + offset;
                    break;

                case "arc":
                    float startAngle = -arcAngle / 2f;
                    float stepAngle = selected.Count > 1 ? arcAngle / (selected.Count - 1) : 0;
                    float currentAngle = startAngle + stepAngle * i;
                    Vector3 arcOffset = Quaternion.Euler(0, currentAngle, 0) * (Vector3.forward * spacing);
                    newPos = startPos + arcOffset;
                    break;
            }
    
            selected[i].transform.position = newPos;
    
            if (lookAtCenter && (layoutType.ToLower() == "circle" || layoutType.ToLower() == "arc"))
            {
                selected[i].transform.LookAt(startPos);
            }
        }

        { result.SetResult(new { success = true, layout = layoutType, count = selected.Count, spacing }); return; }
    }

    private static Vector3 ParseAxis(string axis) =>
        axis?.ToUpper() switch
        {
            "X" => Vector3.right,
            "-X" => Vector3.left,
            "Y" => Vector3.up,
            "-Y" => Vector3.down,
            "Z" => Vector3.forward,
            "-Z" => Vector3.back,
            _ => Vector3.right
        };
}
```
