# smart_align_to_ground

Raycast selected objects downward to snap them to the ground surface. Requires objects selected in Hierarchy first.

**Signature:** `SmartAlignToGround(float maxDistance = 100f, bool alignRotation = false)`

**Returns:** `{ success, aligned, total }`

**Notes:**
- Select objects in Hierarchy before calling.
- Raycast fires from slightly above each object (`+0.1` on Y) to avoid self-hit.
- Objects that miss the ground (no hit within `maxDistance`) are not moved.
- `alignRotation = true` tilts the object so its up-axis matches the surface normal.
- Supports undo and workflow snapshots.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float maxDistance = 100f;     // maximum raycast distance downward
        bool alignRotation = false;   // true = align up-axis to surface normal

        /* Original Logic:

            var selected = Selection.gameObjects;
            if (selected.Length == 0) return new { error = "No objects selected" };
            int aligned = 0;
            foreach (var go in selected)
            {
                WorkflowManager.SnapshotObject(go.transform);
                Undo.RecordObject(go.transform, "Align To Ground");
                if (Physics.Raycast(go.transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, maxDistance))
                {
                    go.transform.position = hit.point;
                    if (alignRotation) go.transform.up = hit.normal;
                    aligned++;
                }
            }
            return new { success = true, aligned, total = selected.Length };
        */
    }
}
```
