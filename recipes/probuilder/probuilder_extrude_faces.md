# probuilder_extrude_faces

Extrude selected faces along their normals.

**Signature:** `ProBuilderExtrudeFaces(string name = null, int instanceId = 0, string path = null, string faceIndexes = null, float distance = 0.5f, string method = "FaceNormal")`

**Returns:** `{ success, name, instanceId, extrudedFaceCount, method, distance, totalFaces, totalVertices }`

## Notes

- `faceIndexes`: comma-separated face indices, e.g. `"0,1,2"`. Omit to extrude all faces.
- `distance`: extrude amount in meters (default `0.5`).
- `method`: `FaceNormal` (default), `IndividualFaces`, `VertexNormal`.
- Call `probuilder_get_info` first to confirm face count before selecting indexes.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        string faceIndexes = "0";
        float distance = 0.5f;
        string method = "FaceNormal";

        var res = UnitySkillsBridge.Call("probuilder_extrude_faces", new {
            name, faceIndexes, distance, method
        });
        result.Log("Extruded: {0}", res);
    }
}
```
