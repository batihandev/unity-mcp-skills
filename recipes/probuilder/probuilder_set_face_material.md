# probuilder_set_face_material

Assign a material to specific faces of a ProBuilder mesh.

**Signature:** `ProBuilderSetFaceMaterial(string name = null, int instanceId = 0, string path = null, string faceIndexes = null, string materialPath = null, int submeshIndex = -1)`

**Returns:** `{ success, name, instanceId, affectedFaces, materialCount }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to apply to all faces.
- Provide either `materialPath` (asset path, e.g. `"Assets/Materials/MyMat.mat"`) or `submeshIndex`.
- When `materialPath` is provided, the material is added to the renderer's shared materials array if not already present.
- `submeshIndex`: use `-1` (default) when providing `materialPath`; otherwise set to an existing renderer slot index.
- Use `probuilder_set_material` to assign a single material to the whole object.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        string faceIndexes = null;
        string materialPath = "Assets/Materials/MyMat.mat";
        int submeshIndex = -1;

        var res = UnitySkillsBridge.Call("probuilder_set_face_material", new {
            name, faceIndexes, materialPath, submeshIndex
        });
        result.Log("Set face material: {0}", res);
    }
}
```
