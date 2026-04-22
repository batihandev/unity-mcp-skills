# navmesh_clear

Clear baked NavMesh data from every `NavMeshSurface` in the active scene. **This operation cannot be undone.**

**Signature:** `NavMeshClear()`

**Returns:** `{ success, surfaces, warning }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

**Requires:** `com.unity.ai.navigation` package.

```csharp
using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        foreach (var s in surfaces) s.RemoveData();
        result.SetResult(new { success = true, surfaces = surfaces.Length, warning = "NavMesh cleared for " + surfaces.Length + " surface(s). This operation cannot be undone." });
    }
}
```
