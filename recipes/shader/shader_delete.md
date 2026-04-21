# shader_delete

Delete a shader asset file from the project.

**Signature:** `ShaderDelete(string shaderPath)`

**Returns:** `{ success, deleted }`

## Notes

- `shaderPath` must be a valid `Assets/`-rooted path to a `.shader` file.
- Returns an error if the file does not exist.
- A workflow snapshot is taken before deletion for undo support.
- The asset is removed via `AssetDatabase.DeleteAsset`.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string shaderPath = "Assets/Shaders/MyShader.shader";

        /* Original Logic:

            if (Validate.SafePath(shaderPath, "shaderPath", isDelete: true) is object pathErr) return pathErr;
            if (!File.Exists(shaderPath))
                return new { error = $"Shader not found: {shaderPath}" };

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(shaderPath);
            if (asset != null) WorkflowManager.SnapshotObject(asset);

            AssetDatabase.DeleteAsset(shaderPath);
            return new { success = true, deleted = shaderPath };
        */
    }
}
```
