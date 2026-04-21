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
        string name = "MyMaterial";
        string shaderName = null;          // null → auto-detect for active pipeline
        string savePath = "Assets/Materials"; // folder or full path; null → memory only

        if (!string.IsNullOrEmpty(savePath) && Validate.SafePath(savePath, "savePath") is object pathErr) { result.SetResult(pathErr); return; }

        // Auto-detect shader based on render pipeline if not specified
        if (string.IsNullOrEmpty(shaderName))
        {
            shaderName = ProjectSkills.GetDefaultShaderName();
        }

        var shader = Shader.Find(shaderName);
        if (shader == null)
        {
            // Try fallback shaders
            var pipeline = ProjectSkills.DetectRenderPipeline();
            var fallbackShaders = pipeline switch
            {
                ProjectSkills.RenderPipelineType.URP => new[] { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Standard" },
                ProjectSkills.RenderPipelineType.HDRP => new[] { "HDRP/Lit", "Standard" },
                _ => new[] { "Standard", "Mobile/Diffuse", "Unlit/Color" }
            };
    
            foreach (var fallback in fallbackShaders)
            {
                shader = Shader.Find(fallback);
                if (shader != null)
                {
                    shaderName = fallback;
                    break;
                }
            }
    
            if (shader == null)
            {
                var pipelineInfo = ProjectSkills.DetectRenderPipeline();
                { result.SetResult(new { 
                    error = $"Shader not found: {shaderName}. Detected pipeline: {pipelineInfo}. Try using project_get_render_pipeline to see available shaders.",
                    detectedPipeline = pipelineInfo.ToString(),
                    recommendedShader = ProjectSkills.GetDefaultShaderName()
                }); return; }
            }
        }

        var material = new Material(shader) { name = name };

        if (!string.IsNullOrEmpty(savePath))
        {
            // Smart path resolution
            savePath = ResolveSavePath(savePath, name);
            EnsureDirectoryExists(savePath);

            AssetDatabase.CreateAsset(material, savePath);
            WorkflowManager.SnapshotObject(material, SnapshotType.Created);
            AssetDatabase.SaveAssets();
        }
        else
        {
            // No savePath: return instanceId so caller can reference or destroy later
            var pipelineType2 = ProjectSkills.DetectRenderPipeline();
            { result.SetResult(new {
                success = true,
                name,
                shader = shaderName,
                path = (string)null,
                instanceId = material.GetInstanceID(),
                renderPipeline = pipelineType2.ToString(),
                colorProperty = ProjectSkills.GetColorPropertyName(),
                textureProperty = ProjectSkills.GetMainTexturePropertyName(),
                warning = "Material created in memory only (no savePath). It will be lost on editor restart. Use asset_save or specify savePath to persist."
            }); return; }
        }

        var pipelineType = ProjectSkills.DetectRenderPipeline();
        { result.SetResult(new {
            success = true,
            name,
            shader = shaderName,
            path = savePath,
            renderPipeline = pipelineType.ToString(),
            colorProperty = ProjectSkills.GetColorPropertyName(),
            textureProperty = ProjectSkills.GetMainTexturePropertyName()
        }); return; }
    }
}
```
