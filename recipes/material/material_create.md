# material_create

Create a new material (auto-detects render pipeline if shader not specified). `savePath` can be a folder or full path.

**Signature:** `MaterialCreate(string name, string shaderName = null, string savePath = null)`

**Returns (with savePath):** `{ success, name, shader, path, renderPipeline, colorProperty, textureProperty }`

**Returns (no savePath):** `{ success, name, shader, path=null, instanceId, renderPipeline, colorProperty, textureProperty, warning }`

## Notes

- `shaderName` is auto-detected from the active render pipeline if omitted (Standard / URP/Lit / HDRP/Lit).
- If the requested shader is not found, a pipeline-appropriate fallback is tried before returning an error.
- Without `savePath` the material is created in memory only — it will be lost on editor restart. Specify `savePath` or call `asset_save` to persist it.
- `savePath` is smart: a folder path or a path without `.mat` extension is resolved automatically.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "MyMaterial";
        string shaderName = null;          // null → try URP/Lit, HDRP/Lit, Standard, Mobile/Diffuse, Unlit/Color
        string savePath = "Assets/Materials"; // folder or full path; null → memory only

        if (!string.IsNullOrEmpty(savePath) && Validate.SafePath(savePath, "savePath") is object pathErr) { result.SetResult(pathErr); return; }

        Shader shader = null;
        if (!string.IsNullOrEmpty(shaderName))
            shader = Shader.Find(shaderName);

        if (shader == null)
        {
            foreach (var candidate in new[] { "Universal Render Pipeline/Lit", "HDRP/Lit", "Standard", "Mobile/Diffuse", "Unlit/Color" })
            {
                var s = Shader.Find(candidate);
                if (s != null) { shader = s; shaderName = candidate; break; }
            }
        }
        if (shader == null)
        {
            result.SetResult(new { error = "No usable shader found. Ensure SRP package is installed." });
            return;
        }

        var pipeline = DetectPipeline();
        var colorProperty = GetColorPropertyName(pipeline);
        var textureProperty = GetMainTexturePropertyName(pipeline);

        var material = new Material(shader) { name = name };

        if (string.IsNullOrEmpty(savePath))
        {
            result.SetResult(new
            {
                success = true,
                name,
                shader = shaderName,
                path = (string)null,
                instanceId = material.GetInstanceID(),
                renderPipeline = pipeline,
                colorProperty,
                textureProperty,
                warning = "Material created in memory only (no savePath). It will be lost on editor restart."
            });
            return;
        }

        // Smart path resolution: folder or full .mat
        if (!savePath.EndsWith(".mat")) savePath = savePath.TrimEnd('/') + "/" + name + ".mat";
        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        AssetDatabase.CreateAsset(material, savePath);
        WorkflowManager.SnapshotObject(material, SnapshotType.Created);
        AssetDatabase.SaveAssets();

        result.SetResult(new
        {
            success = true,
            name,
            shader = shaderName,
            path = savePath,
            renderPipeline = pipeline,
            colorProperty,
            textureProperty
        });
    }

    private static string DetectPipeline()
    {
        var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (rp == null) return "BuiltIn";
        var t = rp.GetType().FullName ?? "";
        if (t.Contains("Universal")) return "URP";
        if (t.Contains("HDRP") || t.Contains("HDRenderPipeline")) return "HDRP";
        return "Custom";
    }

    private static string GetColorPropertyName(string pipeline)
        => (pipeline == "URP" || pipeline == "HDRP") ? "_BaseColor" : "_Color";

    private static string GetMainTexturePropertyName(string pipeline)
    {
        if (pipeline == "URP") return "_BaseMap";
        if (pipeline == "HDRP") return "_BaseColorMap";
        return "_MainTex";
    }
}
```
