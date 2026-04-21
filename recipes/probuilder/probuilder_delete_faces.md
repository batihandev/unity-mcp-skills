# probuilder_delete_faces

Delete faces by index from a ProBuilder mesh.

**Signature:** `ProBuilderDeleteFaces(string name = null, int instanceId = 0, string path = null, string faceIndexes = null)`

**Returns:** `{ success, name, instanceId, deletedCount, remainingFaces, remainingVertices }`

## Notes

- `faceIndexes` is required (comma-separated integers, e.g. `"0,1"`).
- Out-of-range indices are silently skipped; at least one valid index is required.
- Call `probuilder_get_info` first to verify face count.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        string faceIndexes = "0,1";

        var res = UnitySkillsBridge.Call("probuilder_delete_faces", new { name, faceIndexes });
        result.Log("Deleted faces: {0}", res);
    }
}
```
