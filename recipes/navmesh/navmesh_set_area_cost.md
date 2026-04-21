# navmesh_set_area_cost

Set the traversal cost for a NavMesh area. Higher costs make agents prefer cheaper areas. Area index must be 0–31; cost must be >= 0.

**Signature:** `NavMeshSetAreaCost(int areaIndex, float cost)`

**Returns:** `{ success, areaIndex, cost }`

```csharp
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int areaIndex = 3;   // 0–31 (0 = Walkable, 1 = Not Walkable, 2 = Jump, 3+ = custom)
        float cost = 2f;     // >= 0; default is 1.0 for most areas

        if (Validate.InRange(areaIndex, 0, 31, "areaIndex") is object err1) { result.SetResult(err1); return; }
        if (cost < 0) { result.SetResult(new { error = "cost must be >= 0" }); return; }

        NavMesh.SetAreaCost(areaIndex, cost);
        result.SetResult(new { success = true, areaIndex, cost });
    }
}
```
