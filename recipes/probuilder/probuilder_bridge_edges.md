# probuilder_bridge_edges

Connect two edges with a new face on a ProBuilder mesh (create doorways, windows, or connections).

**Signature:** `ProBuilderBridgeEdges(string name = null, int instanceId = 0, string path = null, string edgeA = null, string edgeB = null, bool allowNonManifold = false)`

**Returns:** `{ success, name, instanceId, bridgedEdge: { a, b }, totalFaces, totalVertices }`

## Notes

- `edgeA` and `edgeB` are both required; use vertex-index pairs, e.g. `"0-1"` and `"4-5"`.
- `allowNonManifold`: allow bridging in cases that produce non-manifold geometry (default `false`).
- Returns an error if either edge is not found or the bridge fails.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        string edgeA = "0-1";
        string edgeB = "4-5";
        bool allowNonManifold = false;

        var res = UnitySkillsBridge.Call("probuilder_bridge_edges", new {
            name, edgeA, edgeB, allowNonManifold
        });
        result.Log("Bridged: {0}", res);
    }
}
```
