# uitk_delete_file

Delete a USS or UXML file from the project.

**Signature:** `UitkDeleteFile(filePath string)`

**Returns:** `{ success, deleted }`

**Notes:**
- Fails if the file does not exist.
- Takes a workflow snapshot before deletion for undo support.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string filePath = "Assets/UI/OldStyle.uss";

        if (Validate.SafePath(filePath, "filePath", isDelete: true) is object pathErr) { result.SetResult(pathErr); return; }
        if (!File.Exists(filePath)) { result.SetResult(new { error = $"File not found: {filePath}" }); return; }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        AssetDatabase.MoveAssetToTrash(filePath);
        result.SetResult(new { success = true, deleted = filePath });
    }
}
```
