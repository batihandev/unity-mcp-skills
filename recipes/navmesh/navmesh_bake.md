# navmesh_bake

Bake the NavMesh synchronously. Re-bake after any scene geometry changes. **Warning: Can be slow on large scenes.**

**Signature:** `NavMeshBake()`

**Returns:** `{ success, message }`

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
