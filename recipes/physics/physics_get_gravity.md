# physics_get_gravity

Get global gravity setting.

**Signature:** `PhysicsGetGravity()`

**Returns:** `{ x, y, z }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var g = Physics.gravity;
        result.SetResult(new { x = g.x, y = g.y, z = g.z });
    }
}
```
