# probuilder_get_info

Get face, vertex, edge, material, and bounds info for a ProBuilder mesh.

**Signature:** `ProBuilderGetInfo(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, name, instanceId, isProBuilder, vertexCount, faceCount, edgeCount, triangleCount, shapeType, position, bounds, materials, submeshFaceCounts }`

## Notes

- Read-only — does not modify the mesh.
- Call this before face or vertex edits to confirm indices and topology.
- `shapeType` is detected via reflection (internal `ProBuilderShape` API).
- `submeshFaceCounts` shows how many faces belong to each submesh/material slot.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

**Requires:** `com.unity.probuilder` package.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;
        int instanceId = 0;
        string path = null;

        var (go, findErr) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (findErr != null) { result.SetResult(findErr); return; }

        var pbMesh = go.GetComponent<ProBuilderMesh>();
        if (pbMesh == null)
        {
            result.SetResult(new { error = "GameObject '" + go.name + "' does not have a ProBuilderMesh component" });
            return;
        }

        var renderer = pbMesh.GetComponent<MeshRenderer>();
        var bounds = pbMesh.GetComponent<MeshFilter>()?.sharedMesh?.bounds ?? new Bounds();

        var shapeTypeName = GetShapeTypeName(go);

        var submeshes = new Dictionary<int, int>();
        foreach (var face in pbMesh.faces)
        {
            if (!submeshes.ContainsKey(face.submeshIndex))
                submeshes[face.submeshIndex] = 0;
            submeshes[face.submeshIndex]++;
        }

        result.SetResult(new
        {
            success = true,
            name = go.name,
            instanceId = go.GetInstanceID(),
            isProBuilder = true,
            vertexCount = pbMesh.vertexCount,
            faceCount = pbMesh.faceCount,
            edgeCount = pbMesh.edgeCount,
            triangleCount = pbMesh.triangleCount,
            shapeType = shapeTypeName,
            position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z },
            bounds = new { center = new { x = bounds.center.x, y = bounds.center.y, z = bounds.center.z }, size = new { x = bounds.size.x, y = bounds.size.y, z = bounds.size.z } },
            materials = renderer?.sharedMaterials?.Select((m, i) => new { index = i, name = m != null ? m.name : "(null)" }).ToArray(),
            submeshFaceCounts = submeshes.Select(kv => new { submeshIndex = kv.Key, faceCount = kv.Value }).ToArray()
        });
    }

    private static System.Type _pbShapeType;
    private static System.Reflection.PropertyInfo _pbShapeShapeProp;

    private static string GetShapeTypeName(GameObject go)
    {
        if (_pbShapeType == null)
            _pbShapeType = typeof(ProBuilderMesh).Assembly.GetType("UnityEngine.ProBuilder.Shapes.ProBuilderShape");
        if (_pbShapeType == null) return "Unknown";

        var comp = go.GetComponent(_pbShapeType);
        if (comp == null) return "Unknown";

        if (_pbShapeShapeProp == null)
            _pbShapeShapeProp = _pbShapeType.GetProperty("shape");
        var shape = _pbShapeShapeProp?.GetValue(comp);
        return shape?.GetType().Name ?? "Unknown";
    }
}
```
