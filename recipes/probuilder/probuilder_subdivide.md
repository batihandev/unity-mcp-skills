# probuilder_subdivide

Subdivide faces by connecting edge midpoints on a ProBuilder mesh.

**Signature:** `ProBuilderSubdivide(string name = null, int instanceId = 0, string path = null, string faceIndexes = null)`

**Returns:** `{ success, name, instanceId, totalFaces, totalVertices }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to subdivide all faces.
- Each subdivision pass multiplies face count roughly by 4 — use sparingly on high-poly meshes.
- Use `probuilder_get_info` to check face count before subdividing.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        string faceIndexes = null; // null = all faces

        var res = UnitySkillsBridge.Call("probuilder_subdivide", new { name, faceIndexes });
        result.Log("Subdivided: {0}", res);
    }
}
```
