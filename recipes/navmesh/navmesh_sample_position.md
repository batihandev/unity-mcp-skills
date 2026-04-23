# navmesh_sample_position

Find the nearest point on the NavMesh to a given world-space position. Useful for snapping spawn points or validating positions before moving agents.

**Signature:** `NavMeshSamplePosition(float x, float y, float z, float maxDistance = 10f)`

**Returns:** `{ success, found, point: { x, y, z }, distance }` — `point` and `distance` are absent when `found` is `false`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float x = 5f;
        float y = 0f;
        float z = 5f;
        float maxDistance = 10f;  // Maximum search radius

        var sourcePos = new Vector3(x, y, z);

        if (NavMesh.SamplePosition(sourcePos, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            result.SetResult(new
            {
                success = true,
                found = true,
                point = new { x = hit.position.x, y = hit.position.y, z = hit.position.z },
                distance = hit.distance
            });
        }
        else
        {
            result.SetResult(new { success = true, found = false });
        }
    }
}
```
