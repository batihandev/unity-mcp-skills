# material_duplicate

Duplicate an existing material asset.

**Signature:** `MaterialDuplicate(string sourcePath, string newName, string savePath = null)`

**Returns:** `{ success, name, path, sourcePath, shader }`

## Notes

- `sourcePath` must be a valid `.mat` asset path (e.g. `Assets/Materials/Base.mat`).
- `newName` is the name (without extension) of the duplicated material.
- If `savePath` is omitted, the duplicate is saved in the same folder as `sourcePath`.
- `savePath` accepts a folder or a full path; `.mat` extension is appended automatically.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string sourcePath = "Assets/Materials/Base.mat"; // required
        string newName    = "BaseCopy";                  // required; no extension
        string savePath   = null;                        // null → same folder as source

        /* Original Logic:

            if (Validate.Required(sourcePath, "sourcePath") is object err) return err;
            if (Validate.Required(newName, "newName") is object err2) return err2;
            if (Validate.SafePath(sourcePath, "sourcePath") is object srcErr) return srcErr;
            if (!string.IsNullOrEmpty(savePath) && Validate.SafePath(savePath, "savePath") is object saveErr) return saveErr;

            var sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
            if (sourceMaterial == null)
                return new { error = $"Source material not found: {sourcePath}" };

            var newMaterial = new Material(sourceMaterial) { name = newName };

            if (string.IsNullOrEmpty(savePath))
            {
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

            return new { success = true, name = newName, path = savePath, sourcePath, shader = newMaterial.shader.name };
        */
    }
}
```
