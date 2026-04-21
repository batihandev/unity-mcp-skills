# physics_raycast_all

Cast a ray and return ALL hits (penetrating), ordered by distance.

**Signature:** `PhysicsRaycastAll(float originX, float originY, float originZ, float dirX, float dirY, float dirZ, float maxDistance = 1000f, int layerMask = -1)`

**Returns:** `{ count, hits: [{ objectName, instanceId, path, point, normal, distance }] }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float originX = 0f;
        float originY = 0f;
        float originZ = 0f;
        float dirX = 0f;
        float dirY = -1f;
        float dirZ = 0f;
        float maxDistance = 1000f;
        int layerMask = -1;

        var origin = new Vector3(originX, originY, originZ);
        var direction = new Vector3(dirX, dirY, dirZ);
        if (direction.sqrMagnitude < 1e-6f)
        {
            result.SetResult(new { error = "Direction vector cannot be zero" });
            return;
        }
        direction.Normalize();

        var hits = Physics.RaycastAll(origin, direction, maxDistance, layerMask);
        var results = hits.OrderBy(h => h.distance).Select(h => new
        {
            objectName = h.collider.gameObject.name,
            instanceId = h.collider.gameObject.GetInstanceID(),
            path = GameObjectFinder.GetPath(h.collider.gameObject),
            point = new { x = h.point.x, y = h.point.y, z = h.point.z },
            normal = new { x = h.normal.x, y = h.normal.y, z = h.normal.z },
            distance = h.distance
        }).ToArray();

        result.SetResult(new { count = results.Length, hits = results });
    }
}
```
