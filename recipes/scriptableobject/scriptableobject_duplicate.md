# scriptableobject_duplicate

Duplicate a ScriptableObject asset, generating a unique path for the copy.

**Signature:** `ScriptableObjectDuplicate(string assetPath)`

**Returns:** `{ success, original, copy }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Data/MyAsset.asset"; // Source asset path to duplicate

        if (Validate.SafePath(assetPath, "assetPath") is object pathErr) { result.SetResult(pathErr); return; }

        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (asset == null)
        {
            result.SetResult(new { error = $"ScriptableObject not found: {assetPath}" });
            return;
        }

        var newPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
        AssetDatabase.CopyAsset(assetPath, newPath);

        var newAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(newPath);
        if (newAsset != null)
            WorkflowManager.SnapshotObject(newAsset, SnapshotType.Created);

        result.SetResult(new { success = true, original = assetPath, copy = newPath });
    }
}
```
