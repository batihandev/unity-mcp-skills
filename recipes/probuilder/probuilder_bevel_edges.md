# probuilder_bevel_edges

Chamfer (bevel) edges on a ProBuilder mesh.

**Signature:** `ProBuilderBevelEdges(string name = null, int instanceId = 0, string path = null, string edgeIndexes = null, float amount = 0.2f)`

**Returns:** `{ success, name, instanceId, beveledEdgeCount, newFaceCount, amount, totalFaces, totalVertices }`

## Notes

- `edgeIndexes`: vertex-index pairs, e.g. `"0-1,2-3"`. Omit to bevel all edges.
- `amount`: bevel width factor in range `(0, 1]` (default `0.2`).
- Returns an error if `amount <= 0` or `amount > 1`.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        string edgeIndexes = null; // null = all edges
        float amount = 0.2f;

        var res = UnitySkillsBridge.Call("probuilder_bevel_edges", new { name, edgeIndexes, amount });
        result.Log("Beveled: {0}", res);
    }
}
```
