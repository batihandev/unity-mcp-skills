# asset_move

Move or rename an asset within the project.

**Signature:** `AssetMove(string sourcePath, string destinationPath)`

**Returns:** `{ success, from, to }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string sourcePath = "Assets/Scripts/OldName.cs";
        string destinationPath = "Assets/Scripts/NewName.cs";

        if (Validate.SafePath(sourcePath, "sourcePath") is object err1) { result.SetResult(err1); return; }
        if (Validate.SafePath(destinationPath, "destinationPath") is object err2) { result.SetResult(err2); return; }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(sourcePath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        var error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
        if (!string.IsNullOrEmpty(error)) { result.SetResult(new { error }); return; }

        result.SetResult(new { success = true, from = sourcePath, to = destinationPath });
    }
}
```
