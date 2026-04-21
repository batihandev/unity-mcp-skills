# probuilder_flip_normals

Reverse face winding (flip normals) on a ProBuilder mesh.

**Signature:** `ProBuilderFlipNormals(string name = null, int instanceId = 0, string path = null, string faceIndexes = null)`

**Returns:** `{ success, name, instanceId, flippedCount }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to flip all faces.
- Use when a face appears invisible (back-face culling) and you want to make it face the camera.
- For consistent outward normals across the whole mesh prefer `probuilder_conform_normals`.

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

        var res = UnitySkillsBridge.Call("probuilder_flip_normals", new { name, faceIndexes });
        result.Log("Flipped: {0}", res);
    }
}
```
