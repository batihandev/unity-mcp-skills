# probuilder_project_uv

Box-project UVs onto faces of a ProBuilder mesh.

**Signature:** `ProBuilderProjectUV(string name = null, int instanceId = 0, string path = null, string faceIndexes = null, int channel = 0)`

**Returns:** `{ success, name, instanceId, projectedFaceCount, channel, method }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to project all faces.
- `channel`: UV channel `0`–`3` (default `0`). Channel `1` is the lightmap UV.
- Only box projection is supported; other UV projection modes are not available.
- Uses reflection to access `UVEditing.ProjectFacesBox` (internal in ProBuilder 5.x).

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
        int channel = 0;

        var res = UnitySkillsBridge.Call("probuilder_project_uv", new { name, faceIndexes, channel });
        result.Log("Projected UV: {0}", res);
    }
}
```
