# navmesh_clear

Clear baked NavMesh data from every `NavMeshSurface` in the active scene. **This operation cannot be undone.**

**Signature:** `NavMeshClear()`

**Returns:** `{ success, surfaces, warning }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

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
