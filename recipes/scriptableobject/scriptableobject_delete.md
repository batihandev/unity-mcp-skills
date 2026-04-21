# scriptableobject_delete

Delete a ScriptableObject asset from the project.

**Signature:** `ScriptableObjectDelete(string assetPath)`

**Returns:** `{ success, deleted }`

## Notes

- Path is validated with `isDelete: true` to enforce safe-delete rules.
- A workflow snapshot is taken before deletion.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Data/MyAsset.asset"; // Asset path of the ScriptableObject to delete

        if (Validate.SafePath(assetPath, "assetPath", isDelete: true) is object pathErr) { result.SetResult(pathErr); return; }

        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (asset == null)
        {
            result.SetResult(new { error = $"ScriptableObject not found: {assetPath}" });
            return;
        }

        WorkflowManager.SnapshotObject(asset);
        AssetDatabase.DeleteAsset(assetPath);
        result.SetResult(new { success = true, deleted = assetPath });
    }
}
```
