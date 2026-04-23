# probuilder_create_shape

Create a parametric ProBuilder shape in the scene.

**Signature:** `ProBuilderCreateShape(string shape = "Cube", string name = null, float x = 0, float y = 0, float z = 0, float sizeX = 1, float sizeY = 1, float sizeZ = 1, float rotX = 0, float rotY = 0, float rotZ = 0, string parent = null)`

**Returns:** `{ success, name, instanceId, shape, position: { x, y, z }, size: { x, y, z }, vertexCount, faceCount }`

## Notes

- Supported shapes: `Cube`, `Sphere`, `Cylinder`, `Cone`, `Torus`, `Prism`, `Arch`, `Pipe`, `Stairs`, `Door`, `Plane`.
- `x/y/z` is the **center** of the shape, not the base. Floor top at `0` with `sizeY=0.3` means `y = -0.15`.
- Size is baked into vertices (scale is frozen after creation).
- For 2+ shapes prefer `probuilder_create_batch`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

**Requires:** `com.unity.probuilder` package.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.ProBuilder.Shapes;
using System;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shape = "Cube";
        string name = null;
        float x = 0, y = 0, z = 0;
        float sizeX = 1, sizeY = 1, sizeZ = 1;
        float rotX = 0, rotY = 0, rotZ = 0;
        string parent = null;

        if (!ShapeTypeMap.TryGetValue(shape, out var shapeType))
        { result.SetResult(new { error = "Unknown shape: " + shape + ". Available: " + string.Join(", ", ShapeTypeMap.Keys) }); return; }

        var pbMesh = CreatePBShape(shapeType, name, new Vector3(x, y, z), new Vector3(sizeX, sizeY, sizeZ), new Vector3(rotX, rotY, rotZ), parent);
        if (pbMesh == null)
        { result.SetResult(new { error = "Failed to create ProBuilder shape: " + shape }); return; }

        var go = pbMesh.gameObject;

        Undo.RegisterCreatedObjectUndo(go, "Create ProBuilder Shape");
        WorkflowManager.SnapshotObject(go, SnapshotType.Created);

        result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            shape,
            position = new { x, y, z },
            size = new { x = sizeX, y = sizeY, z = sizeZ },
            vertexCount = pbMesh.vertexCount,
            faceCount = pbMesh.faceCount
        });
    }

    private static readonly Dictionary<string, Type> ShapeTypeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
    {
        { "Cube", typeof(Cube) }, { "Sphere", typeof(Sphere) }, { "Cylinder", typeof(Cylinder) },
        { "Cone", typeof(Cone) }, { "Torus", typeof(Torus) }, { "Prism", typeof(Prism) },
        { "Arch", typeof(Arch) }, { "Pipe", typeof(Pipe) }, { "Stairs", typeof(Stairs) },
        { "Door", typeof(Door) }, { "Plane", typeof(UnityEngine.ProBuilder.Shapes.Plane) },
    };

    private static ProBuilderMesh CreatePBShape(Type shapeType, string objName, Vector3 pos, Vector3 size, Vector3 rot, string parentName)
    {
        var pbMesh = ShapeFactory.Instantiate(shapeType);
        if (pbMesh == null) return null;

        var go = pbMesh.gameObject;
        if (!string.IsNullOrEmpty(objName)) go.name = objName;

        go.transform.localScale = size;
        pbMesh.FreezeScaleTransform();
        pbMesh.ToMesh();
        pbMesh.Refresh();

        go.transform.position = pos;
        go.transform.eulerAngles = rot;

        if (!string.IsNullOrEmpty(parentName))
        {
            var parent = GameObjectFinder.Find(name: parentName);
            if (parent != null) go.transform.SetParent(parent.transform, true);
        }

        return pbMesh;
    }
}
```
