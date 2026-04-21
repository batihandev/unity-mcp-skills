# scriptableobject_duplicate

Duplicate a ScriptableObject asset, generating a unique path for the copy.

**Signature:** `ScriptableObjectDuplicate(string assetPath)`

**Returns:** `{ success, original, copy }`

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

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
