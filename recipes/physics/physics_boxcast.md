# physics_boxcast

Cast a box along a direction and get hit info.

**Signature:** `PhysicsBoxCast(float originX, float originY, float originZ, float dirX, float dirY, float dirZ, float halfExtentX = 0.5f, float halfExtentY = 0.5f, float halfExtentZ = 0.5f, float maxDistance = 1000f, int layerMask = -1)`

**Returns:** `{ hit, objectName, instanceId, point, distance }`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float originX = 0f;
        float originY = 5f;
        float originZ = 0f;
        float dirX = 0f;
        float dirY = -1f;
        float dirZ = 0f;
        float halfExtentX = 0.5f;
        float halfExtentY = 0.5f;
        float halfExtentZ = 0.5f;
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

        var halfExtents = new Vector3(halfExtentX, halfExtentY, halfExtentZ);
        if (Physics.BoxCast(origin, halfExtents, direction, out RaycastHit hit, Quaternion.identity, maxDistance, layerMask))
        {
            result.SetResult(new
            {
                hit = true,
                objectName = hit.collider.gameObject.name,
                instanceId = hit.collider.gameObject.GetInstanceID(),
                point = new { x = hit.point.x, y = hit.point.y, z = hit.point.z },
                distance = hit.distance
            });
            return;
        }
        result.SetResult(new { hit = false });
    }
}
```
