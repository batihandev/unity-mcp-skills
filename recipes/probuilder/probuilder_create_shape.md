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

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shape = "Cube";
        string name = "MyShape";
        float x = 0f, y = 0f, z = 0f;
        float sizeX = 1f, sizeY = 1f, sizeZ = 1f;
        float rotX = 0f, rotY = 0f, rotZ = 0f;
        string parent = null;

        var res = UnitySkillsBridge.Call("probuilder_create_shape", new {
            shape, name, x, y, z, sizeX, sizeY, sizeZ, rotX, rotY, rotZ, parent
        });
        result.Log("Created shape: {0}", res);
    }
}
```
