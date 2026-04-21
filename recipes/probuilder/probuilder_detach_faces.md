# probuilder_detach_faces

Detach faces from shared vertices, creating independent geometry on a ProBuilder mesh.

**Signature:** `ProBuilderDetachFaces(string name = null, int instanceId = 0, string path = null, string faceIndexes = null, bool deleteSourceFaces = false)`

**Returns:** `{ success, name, instanceId, detachedFaceCount, deleteSourceFaces, totalFaces, totalVertices }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to detach all faces.
- `deleteSourceFaces`: if `true`, removes the original faces after detach (default `false`).
- Detached faces share no vertices with adjacent faces, breaking smooth shading across the boundary.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        string faceIndexes = "1";
        bool deleteSourceFaces = false;

        var res = UnitySkillsBridge.Call("probuilder_detach_faces", new {
            name, faceIndexes, deleteSourceFaces
        });
        result.Log("Detached: {0}", res);
    }
}
```
