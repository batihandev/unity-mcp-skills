# light_add_reflection_probe

Create a new Reflection Probe GameObject at a specified position.

**Signature:** `LightAddReflectionProbe(string probeName = "ReflectionProbe", float x = 0, float y = 1, float z = 0, float sizeX = 10, float sizeY = 10, float sizeZ = 10, int resolution = 256)`

**Returns:** `{ success, name, instanceId, resolution, size: { x, y, z } }`

**Notes:**
- `resolution` must be a power of two (e.g. 128, 256, 512, 1024).
- The probe is baked/realtime depending on project settings; this skill only creates the GameObject.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string probeName = "RoomReflectionProbe";
        float x = 0f, y = 1f, z = 0f;
        float sizeX = 10f, sizeY = 5f, sizeZ = 10f;
        int resolution = 256;

        var go = new GameObject(probeName);
        go.transform.position = new Vector3(x, y, z);
        var probe = go.AddComponent<ReflectionProbe>();
        probe.size = new Vector3(sizeX, sizeY, sizeZ);
        probe.resolution = resolution;

        Undo.RegisterCreatedObjectUndo(go, "Create Reflection Probe");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            resolution,
            size = new { x = sizeX, y = sizeY, z = sizeZ }
        });
    }
}
```
