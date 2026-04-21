# probuilder_combine_meshes

Merge multiple ProBuilder meshes into one for optimization.

**Signature:** `ProBuilderCombineMeshes(string names = null)`

**Returns:** `{ success, name, instanceId, combinedCount, resultMeshCount, vertexCount, faceCount }`

## Notes

- `names`: comma-separated GameObject names, e.g. `"Wall_A,Wall_B"`. Use `"selected"` to combine the current editor selection.
- Requires at least 2 ProBuilder meshes.
- Source meshes (all except the first) are destroyed after combining. This operation is undoable.
- The combined result is placed on the first named object.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string names = "Wall_A,Wall_B";

        var res = UnitySkillsBridge.Call("probuilder_combine_meshes", new { names });
        result.Log("Combined: {0}", res);
    }
}
```
