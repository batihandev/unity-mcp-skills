# navmesh_clear

Clear all NavMesh data from the scene. **This operation cannot be undone.**

**Signature:** `NavMeshClear()`

**Returns:** `{ success, warning }`

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.AI;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        NavMeshBuilder.ClearAllNavMeshes();
        result.SetResult(new { success = true, warning = "NavMesh cleared. This operation cannot be undone." });
    }
}
```
