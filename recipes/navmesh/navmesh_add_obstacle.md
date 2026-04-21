# navmesh_add_obstacle

Add a `NavMeshObstacle` component to an existing GameObject. Carving is enabled by default so the obstacle cuts a hole in the NavMesh at runtime.

**Signature:** `NavMeshAddObstacle(string name = null, int instanceId = 0, string path = null, bool carve = true)`

**Returns:** `{ success, gameObject, carving }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

```csharp
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Provide at least one of: name, instanceId, or path
        string name = "Wall";
        int instanceId = 0;
        string path = null;

        bool carve = true;  // true = obstacle carves a hole in the NavMesh at runtime

        var (go, err) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (err != null) { result.SetResult(err); return; }

        if (go.GetComponent<NavMeshObstacle>() != null)
        {
            result.SetResult(new { error = $"{go.name} already has NavMeshObstacle" });
            return;
        }

        var obs = Undo.AddComponent<NavMeshObstacle>(go);
        obs.carving = carve;
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);
        result.SetResult(new { success = true, gameObject = go.name, carving = obs.carving });
    }
}
```
