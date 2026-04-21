# physics_overlap_box

Check for colliders overlapping a box volume.

**Signature:** `PhysicsOverlapBox(float x, float y, float z, float halfExtentX = 0.5f, float halfExtentY = 0.5f, float halfExtentZ = 0.5f, int layerMask = -1)`

**Returns:** `{ count, colliders: [{ objectName, path, isTrigger }] }`

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
        float x = 0f;
        float y = 0f;
        float z = 0f;
        float halfExtentX = 0.5f;
        float halfExtentY = 0.5f;
        float halfExtentZ = 0.5f;
        int layerMask = -1;

        var center = new Vector3(x, y, z);
        var halfExtents = new Vector3(halfExtentX, halfExtentY, halfExtentZ);
        var colliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, layerMask);

        var results = colliders.Select(c => new
        {
            objectName = c.gameObject.name,
            path = GameObjectFinder.GetPath(c.gameObject),
            isTrigger = c.isTrigger
        }).ToArray();

        result.SetResult(new { count = results.Length, colliders = results });
    }
}
```
