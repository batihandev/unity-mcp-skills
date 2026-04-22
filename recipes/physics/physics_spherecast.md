# physics_spherecast

Cast a sphere along a direction and get hit info.

**Signature:** `PhysicsSphereCast(float originX, float originY, float originZ, float dirX, float dirY, float dirZ, float radius, float maxDistance = 1000f, int layerMask = -1)`

**Returns:** `{ hit, objectName, instanceId, point, distance }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

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
        float radius = 0.5f;
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

        if (Physics.SphereCast(origin, radius, direction, out RaycastHit hit, maxDistance, layerMask))
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
