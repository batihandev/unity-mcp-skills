# asset_delete

Delete an asset from the project.

**Signature:** `AssetDelete(string assetPath)`

**Returns:** `{ success, deleted }`

## Notes

- Uses `AssetDatabase.MoveAssetToTrash` (restorable from OS trash). `AssetDatabase.DeleteAsset` is rejected by the Unity_RunCommand MCP analyzer.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.SnapshotObject`

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Textures/old_texture.png";

        if (Validate.SafePath(assetPath, "assetPath", isDelete: true) is object err) { result.SetResult(err); return; }
        if (!File.Exists(assetPath) && !Directory.Exists(assetPath))
        { result.SetResult(new { error = $"Asset not found: {assetPath}" }); return; }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        if (!AssetDatabase.MoveAssetToTrash(assetPath))
        { result.SetResult(new { error = "Delete failed: " + assetPath }); return; }

        result.SetResult(new { success = true, deleted = assetPath });
    }
}
```
