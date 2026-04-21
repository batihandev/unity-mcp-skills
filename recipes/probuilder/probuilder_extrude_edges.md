# probuilder_extrude_edges

Extrude open edges outward on a ProBuilder mesh to create walls, rails, or flanges.

**Signature:** `ProBuilderExtrudeEdges(string name = null, int instanceId = 0, string path = null, string edgeIndexes = null, float distance = 0.5f, bool extrudeAsGroup = true, bool enableManifoldExtrude = false)`

**Returns:** `{ success, name, instanceId, extrudedEdgeCount, newEdgeCount, distance, extrudeAsGroup, totalFaces, totalVertices }`

## Notes

- `edgeIndexes` is required; use vertex-index pairs, e.g. `"0-1,2-3"`.
- `distance`: extrude amount in meters (default `0.5`).
- `extrudeAsGroup`: extrude connected edges as a single group (default `true`).
- `enableManifoldExtrude`: allow manifold edge extrusion (default `false`).

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        string edgeIndexes = "0-1";
        float distance = 0.5f;
        bool extrudeAsGroup = true;

        var res = UnitySkillsBridge.Call("probuilder_extrude_edges", new {
            name, edgeIndexes, distance, extrudeAsGroup
        });
        result.Log("Extruded edges: {0}", res);
    }
}
```
