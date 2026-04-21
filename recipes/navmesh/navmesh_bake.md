# navmesh_bake

Bake the NavMesh synchronously. Re-bake after any scene geometry changes. **Warning: Can be slow on large scenes.**

**Signature:** `NavMeshBake()`

**Returns:** `{ success, message }`

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
        NavMeshBuilder.BuildNavMesh();
        result.SetResult(new { success = true, message = "NavMesh baked successfully" });
    }
}
```
