# material_duplicate

Duplicate an existing material asset.

**Signature:** `MaterialDuplicate(string sourcePath, string newName, string savePath = null)`

**Returns:** `{ success, name, path, sourcePath, shader }`

## Notes

- `sourcePath` must be a valid `.mat` asset path (e.g. `Assets/Materials/Base.mat`).
- `newName` is the name (without extension) of the duplicated material.
- If `savePath` is omitted, the duplicate is saved in the same folder as `sourcePath`.
- `savePath` accepts a folder or a full path; `.mat` extension is appended automatically.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string sourcePath = "Assets/Materials/Base.mat"; // required
        string newName    = "BaseCopy";                  // required; no extension
        string savePath   = null;                        // null → same folder as source

        if (Validate.Required(sourcePath, "sourcePath") is object err) { result.SetResult(err); return; }
        if (Validate.Required(newName, "newName") is object err2) { result.SetResult(err2); return; }
        if (Validate.SafePath(sourcePath, "sourcePath") is object srcErr) { result.SetResult(srcErr); return; }
        if (!string.IsNullOrEmpty(savePath) && Validate.SafePath(savePath, "savePath") is object saveErr) { result.SetResult(saveErr); return; }

        var sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
        if (sourceMaterial == null)
            { result.SetResult(new { error = $"Source material not found: {sourcePath}" }); return; }

        var newMaterial = new Material(sourceMaterial) { name = newName };

        if (string.IsNullOrEmpty(savePath))
        {
            // Save in same folder as source
            var sourceDir = Path.GetDirectoryName(sourcePath);
            savePath = Path.Combine(sourceDir, newName + ".mat").Replace("\\", "/");
        }
        else
        {
            savePath = ResolveSavePath(savePath, newName);
        }

        EnsureDirectoryExists(savePath);
        AssetDatabase.CreateAsset(newMaterial, savePath);
        WorkflowManager.SnapshotObject(newMaterial, SnapshotType.Created);
        AssetDatabase.SaveAssets();

        { result.SetResult(new { 
            success = true, 
            name = newName, 
            path = savePath,
            sourcePath,
            shader = newMaterial.shader.name
        }); return; }
    }

    private static string ResolveSavePath(string savePath, string name)
    {
        if (!savePath.EndsWith(".mat")) savePath = savePath.TrimEnd('/') + "/" + name + ".mat";
        return savePath.Replace("\\", "/");
    }

    private static void EnsureDirectoryExists(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }
}
```
