# physics_set_layer_collision

Set whether two layers collide.

**Signature:** `PhysicsSetLayerCollision(int layer1, int layer2, bool enableCollision = true)`

**Returns:** `{ success, layer1, layer2, collisionEnabled }`

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
        int layer1 = 0;
        int layer2 = 8;
        bool enableCollision = true;

        Physics.IgnoreLayerCollision(layer1, layer2, !enableCollision);
        result.SetResult(new { success = true, layer1, layer2, collisionEnabled = enableCollision });
    }
}
```
