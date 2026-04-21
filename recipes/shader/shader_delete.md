# shader_delete

Delete a shader asset file from the project.

**Signature:** `ShaderDelete(string shaderPath)`

**Returns:** `{ success, deleted }`

## Notes

- `shaderPath` must be a valid `Assets/`-rooted path to a `.shader` file.
- Returns an error if the file does not exist.
- A workflow snapshot is taken before deletion for undo support.
- The asset is removed via `AssetDatabase.DeleteAsset`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

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

        AssetDatabase.DeleteAsset(shaderPath);
        { result.SetResult(new { success = true, deleted = shaderPath }); return; }
    }
}
```
