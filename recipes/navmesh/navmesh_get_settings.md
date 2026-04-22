# navmesh_get_settings

Get the NavMesh build settings for the default agent (index 0). Returns agent radius, height, max slope, and step height.

**Signature:** `NavMeshGetSettings()`

**Returns:** `{ success, agentRadius, agentHeight, agentSlope, agentClimb }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var settings = NavMesh.GetSettingsByIndex(0);
        result.SetResult(new
        {
            success = true,
            agentRadius = settings.agentRadius,
            agentHeight = settings.agentHeight,
            agentSlope = settings.agentSlope,
            agentClimb = settings.agentClimb
        });
    }
}
```
