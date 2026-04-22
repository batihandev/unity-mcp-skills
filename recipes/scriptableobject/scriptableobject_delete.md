# scriptableobject_delete

Delete a ScriptableObject asset from the project.

**Signature:** `ScriptableObjectDelete(string assetPath)`

**Returns:** `{ success, deleted }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

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
        bool moved = AssetDatabase.MoveAssetToTrash(assetPath);
        if (!moved) { result.SetResult(new { error = $"Failed to delete: {assetPath}" }); return; }
        result.SetResult(new { success = true, deleted = assetPath });
    }
}
```
