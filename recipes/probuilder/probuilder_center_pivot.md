# probuilder_center_pivot

Center the pivot to mesh bounds, or move it to a specific world position.

**Signature:** `ProBuilderCenterPivot(string name = null, int instanceId = 0, string path = null, float? worldX = null, float? worldY = null, float? worldZ = null)`

**Returns:** `{ success, name, instanceId, pivot: { x, y, z } }`

## Notes

- Omit `worldX/Y/Z` to auto-center the pivot at the mesh bounds center.
- Provide any combination of `worldX`, `worldY`, `worldZ` to pin individual axes while auto-centering the rest.
- Adjusting the pivot shifts the transform position without moving the visual mesh in the scene.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        #if !PROBUILDER
                    { result.SetResult(NoProBuilder()); return; }
        #else
                    var (pbMesh, err) = FindProBuilderMesh(name, instanceId, path);
                    if (err != null) { result.SetResult(err); return; }

                    Undo.RecordObject(pbMesh.transform, "Center Pivot");
                    Undo.RecordObject(pbMesh, "Center Pivot");
                    WorkflowManager.SnapshotObject(pbMesh.gameObject);

                    if (worldX.HasValue || worldY.HasValue || worldZ.HasValue)
                    {
                        var pos = pbMesh.transform.position;
                        var worldPos = new Vector3(worldX ?? pos.x, worldY ?? pos.y, worldZ ?? pos.z);
                        pbMesh.SetPivot(worldPos);
                    }
                    else
                    {
                        pbMesh.CenterPivot(null);
                    }

                    pbMesh.ToMesh();
                    pbMesh.Refresh();

                    var newPos = pbMesh.transform.position;
                    { result.SetResult(new
                    {
                        success = true,
                        name = pbMesh.gameObject.name,
                        instanceId = pbMesh.gameObject.GetInstanceID(),
                        pivot = new { x = newPos.x, y = newPos.y, z = newPos.z }
                    }); return; }
        #endif
    }
}
```
