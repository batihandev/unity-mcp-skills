# navmesh_calculate_path

Calculate a path between two world-space points on the NavMesh. Returns the path status, total distance, and corner waypoints.

**Signature:** `NavMeshCalculatePath(float startX, float startY, float startZ, float endX, float endY, float endZ, int areaMask = NavMesh.AllAreas)`

**Returns:** `{ status, valid, distance, cornerCount, corners }`

- `status`: `"PathComplete"`, `"PathPartial"`, or `"PathInvalid"`
- `valid`: `true` only when `status == "PathComplete"`
- `corners`: array of `{ x, y, z }` waypoints

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float startX = 0f;
        float startY = 0f;
        float startZ = 0f;
        float endX = 10f;
        float endY = 0f;
        float endZ = 10f;
        int areaMask = NavMesh.AllAreas;

        var startPos = new Vector3(startX, startY, startZ);
        var endPos = new Vector3(endX, endY, endZ);

        NavMeshPath path = new NavMeshPath();
        bool hasPath = NavMesh.CalculatePath(startPos, endPos, areaMask, path);

        if (!hasPath)
        {
            result.SetResult(new { status = "NoPath", valid = false });
            return;
        }

        float distance = 0f;
        if (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial)
        {
            for (int i = 0; i < path.corners.Length - 1; i++)
                distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }

        var corners = new System.Collections.Generic.List<object>();
        foreach (var c in path.corners)
            corners.Add(new { x = c.x, y = c.y, z = c.z });

        result.SetResult(new
        {
            status = path.status.ToString(),
            valid = path.status == NavMeshPathStatus.PathComplete,
            distance,
            cornerCount = path.corners.Length,
            corners
        });
    }
}
```
