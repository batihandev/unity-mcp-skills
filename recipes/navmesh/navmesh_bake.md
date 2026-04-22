# navmesh_bake

Bake every `NavMeshSurface` in the active scene. Per-surface bake — the scene must contain at least one `NavMeshSurface` component. Re-bake after geometry changes. **Warning: Can be slow on large scenes.**

**Signature:** `NavMeshBake()`

**Returns:** `{ success, surfaces, message }`

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
        if (surfaces.Length == 0)
        {
            result.SetResult(new { error = "No NavMeshSurface components found in the active scene. Add a NavMeshSurface to a scene root GameObject before baking." });
            return;
        }
        foreach (var s in surfaces) s.BuildNavMesh();
        result.SetResult(new { success = true, surfaces = surfaces.Length, message = "NavMesh baked for " + surfaces.Length + " surface(s)." });
    }
}
```
