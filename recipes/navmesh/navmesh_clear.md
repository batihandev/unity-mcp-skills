# navmesh_clear

Clear all NavMesh data from the scene. **This operation cannot be undone.**

**Signature:** `NavMeshClear()`

**Returns:** `{ success, warning }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

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
