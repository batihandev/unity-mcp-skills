# probuilder_merge_faces

Merge 2 or more faces into a single face on a ProBuilder mesh.

**Signature:** `ProBuilderMergeFaces(string name = null, int instanceId = 0, string path = null, string faceIndexes = null)`

**Returns:** `{ success, name, instanceId, mergedFromCount, totalFaces, totalVertices }`

## Notes

- `faceIndexes`: comma-separated face indices (at least 2 required), e.g. `"2,3"`. Omit to merge all faces.
- Returns an error if fewer than 2 valid faces are selected.
- Useful to clean up over-subdivided areas or combine coplanar faces.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyShape";
        string faceIndexes = "2,3";

        var res = UnitySkillsBridge.Call("probuilder_merge_faces", new { name, faceIndexes });
        result.Log("Merged: {0}", res);
    }
}
```
