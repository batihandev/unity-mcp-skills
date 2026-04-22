# shader_delete

Move a shader asset file to the OS trash (restorable).

**Signature:** `ShaderDelete(string shaderPath)`

**Returns:** `{ success, deleted }`

## Notes

- `shaderPath` must be a valid `Assets/`-rooted path to a `.shader` file.
- Uses `AssetDatabase.MoveAssetToTrash` — restorable from the OS trash. The Unity_RunCommand analyzer rejects any module containing `AssetDatabase.DeleteAsset`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.SnapshotObject`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shaderPath = "Assets/Shaders/MyShader.shader";

        if (Validate.SafePath(shaderPath, "shaderPath", isDelete: true) is object pathErr) { result.SetResult(pathErr); return; }
        if (!File.Exists(shaderPath))
        { result.SetResult(new { error = $"Shader not found: {shaderPath}" }); return; }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(shaderPath);
        if (asset != null) WorkflowManager.SnapshotObject(asset);

        if (!AssetDatabase.MoveAssetToTrash(shaderPath))
        { result.SetResult(new { error = "Delete failed: " + shaderPath }); return; }

        result.SetResult(new { success = true, deleted = shaderPath });
    }
}
```
