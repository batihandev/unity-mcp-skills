# material_create_batch

Create multiple materials in a single call (efficient batch operation).

**Signature:** `MaterialCreateBatch(string items)`

**`items`:** JSON array of `{ name, shaderName?, savePath? }` objects.

**Returns:** `{ success, totalItems, successCount, failCount, results: [{ success, name, path }] }`

## Notes

- Each item delegates to `material_create` internally; pipeline auto-detection applies per item.
- Prefer this over calling `material_create` repeatedly when creating 2+ materials.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

internal sealed class _BatchMaterialCreateItem
{
    public string name;
    public string shaderName;
    public string savePath;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchMaterialCreateItem { name = "RedMat", savePath = "Assets/Materials" },
            new _BatchMaterialCreateItem { name = "BlueMat", shaderName = "Universal Render Pipeline/Lit", savePath = "Assets/Materials" },
            new _BatchMaterialCreateItem { name = "GreenMat", savePath = "Assets/Materials" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var shaderName = string.IsNullOrEmpty(item.shaderName) ? FindBestShaderName() : item.shaderName;
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                results.Add(new { name = item.name, success = false, error = "Shader not found: " + shaderName });
                failCount++;
                continue;
            }

            var mat = new Material(shader) { name = item.name };

            string savedPath = null;
            if (!string.IsNullOrEmpty(item.savePath))
            {
                var dir = item.savePath;
                if (!dir.EndsWith(".mat"))
                {
                    if (!AssetDatabase.IsValidFolder(dir))
                        Directory.CreateDirectory(dir);
                    savedPath = dir.TrimEnd('/') + "/" + item.name + ".mat";
                }
                else savedPath = item.savePath;

                AssetDatabase.CreateAsset(mat, savedPath);
                WorkflowManager.SnapshotObject(mat, SnapshotType.Created);
                AssetDatabase.SaveAssets();
            }

            results.Add(new
            {
                name = item.name,
                success = true,
                shader = shaderName,
                path = savedPath,
                instanceId = mat.GetInstanceID(),
            });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }

    private static string FindBestShaderName()
    {
        foreach (var n in new[] { "Universal Render Pipeline/Lit", "HDRP/Lit", "Standard", "Mobile/Diffuse", "Unlit/Color" })
            if (Shader.Find(n) != null) return n;
        return "Standard";
    }
}
```
