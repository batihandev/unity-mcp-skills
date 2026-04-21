# physics_raycast

Cast a ray and get hit info.

**Signature:** `PhysicsRaycast(float originX, float originY, float originZ, float dirX, float dirY, float dirZ, float maxDistance = 1000f, int layerMask = -1)`

**Returns:** `{ hit, collider, colliderInstanceId, objectName, objectInstanceId, path, point, normal, distance }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEditor;

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

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
        {
            result.SetResult(new
            {
                hit = true,
                collider = hit.collider.name,
                colliderInstanceId = hit.collider.GetInstanceID(),
                objectName = hit.collider.gameObject.name,
                objectInstanceId = hit.collider.gameObject.GetInstanceID(),
                path = GameObjectFinder.GetPath(hit.collider.gameObject),
                point = new { x = hit.point.x, y = hit.point.y, z = hit.point.z },
                normal = new { x = hit.normal.x, y = hit.normal.y, z = hit.normal.z },
                distance = hit.distance
            });
            return;
        }
        result.SetResult(new { hit = false });
    }
}
```
