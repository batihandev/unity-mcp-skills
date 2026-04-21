# probuilder_create_shape

Create a parametric ProBuilder shape in the scene.

**Signature:** `ProBuilderCreateShape(string shape = "Cube", string name = null, float x = 0, float y = 0, float z = 0, float sizeX = 1, float sizeY = 1, float sizeZ = 1, float rotX = 0, float rotY = 0, float rotZ = 0, string parent = null)`

**Returns:** `{ success, name, instanceId, shape, position: { x, y, z }, size: { x, y, z }, vertexCount, faceCount }`

## Notes

- Supported shapes: `Cube`, `Sphere`, `Cylinder`, `Cone`, `Torus`, `Prism`, `Arch`, `Pipe`, `Stairs`, `Door`, `Plane`.
- `x/y/z` is the **center** of the shape, not the base. Floor top at `0` with `sizeY=0.3` means `y = -0.15`.
- Size is baked into vertices (scale is frozen after creation).
- For 2+ shapes prefer `probuilder_create_batch`.
- Requires `com.unity.probuilder` package.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        #if !PROBUILDER
                    { result.SetResult(NoProBuilder()); return; }
        #else
                    if (!ShapeTypeMap.TryGetValue(shape, out var shapeType))
                        { result.SetResult(new { error = $"Unknown shape: {shape}. Available: {string.Join(", ", ShapeTypeMap.Keys)}" }); return; }

                    var pbMesh = CreatePBShape(shapeType, name, new Vector3(x, y, z), new Vector3(sizeX, sizeY, sizeZ), new Vector3(rotX, rotY, rotZ), parent);
                    if (pbMesh == null)
                        { result.SetResult(new { error = $"Failed to create ProBuilder shape: {shape}" }); return; }

                    var go = pbMesh.gameObject;

                    Undo.RegisterCreatedObjectUndo(go, "Create ProBuilder Shape");
                    WorkflowManager.SnapshotObject(go, SnapshotType.Created);

                    { result.SetResult(new
                    {
                        success = true,
                        name = go.name,
                        instanceId = go.GetInstanceID(),
                        shape,
                        position = new { x, y, z },
                        size = new { x = sizeX, y = sizeY, z = sizeZ },
                        vertexCount = pbMesh.vertexCount,
                        faceCount = pbMesh.faceCount
                    }); return; }
        #endif
    }
}
```
