# physics_get_layer_collision

Get whether two layers collide.

**Signature:** `PhysicsGetLayerCollision(int layer1, int layer2)`

**Returns:** `{ layer1, layer2, collisionEnabled }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int layer1 = 0;
        int layer2 = 8;

        bool ignored = Physics.GetIgnoreLayerCollision(layer1, layer2);
        result.SetResult(new { layer1, layer2, collisionEnabled = !ignored });
    }
}
```
