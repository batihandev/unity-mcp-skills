# shader_delete

Move a shader asset file to the OS trash (restorable).

**Signature:** `ShaderDelete(string shaderPath)`

**Returns:** `{ success, deleted }`

## Notes
- Uses `AssetDatabase.MoveAssetToTrash` — restorable from the OS trash. The Unity_RunCommand analyzer rejects any module containing `AssetDatabase.DeleteAsset`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

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
