# probuilder_conform_normals

Make face normals point consistently outward on a ProBuilder mesh.

**Signature:** `ProBuilderConformNormals(string name = null, int instanceId = 0, string path = null, string faceIndexes = null)`

**Returns:** `{ success, name, instanceId, status, notification, faceCount }`

## Notes

- `faceIndexes`: comma-separated face indices. Omit to conform all faces.
- Unlike `probuilder_flip_normals`, this operation detects the correct outward direction rather than simply reversing winding.
- `status` reflects the ProBuilder `ActionResult` status string.

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

        var res = UnitySkillsBridge.Call("probuilder_conform_normals", new { name, faceIndexes });
        result.Log("Conformed: {0}", res);
    }
}
```
