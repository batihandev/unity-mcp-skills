# project_get_render_pipeline

Get the current render pipeline type and probe which common shaders are available in this project. Read-only; no parameters required.

**Signature:** `ProjectGetRenderPipeline()`

**Returns:** `{ success, pipelineType, pipelineName, defaultShader, unlitShader, colorProperty, textureProperty, availableShaders: [{ name, available }] }`

## Notes

- `pipelineType` is one of `BuiltIn`, `URP`, `HDRP`, or `Custom`.
- `availableShaders` lists the canonical shaders for the detected pipeline and whether each was found via `Shader.Find`.
- Use `colorProperty` and `textureProperty` when setting material properties to avoid hardcoding pipeline-specific names.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var currentRP = GraphicsSettings.currentRenderPipeline;
        string pipelineType = currentRP == null ? "BuiltIn"
            : currentRP.GetType().Name.Contains("Universal") || currentRP.GetType().Name.Contains("URP") ? "URP"
            : currentRP.GetType().Name.Contains("HDRender") || currentRP.GetType().Name.Contains("HDRP") ? "HDRP"
            : "Custom";

        string defaultShader = pipelineType == "URP" ? "Universal Render Pipeline/Lit"
            : pipelineType == "HDRP" ? "HDRP/Lit" : "Standard";
        string unlitShader = pipelineType == "URP" ? "Universal Render Pipeline/Unlit"
            : pipelineType == "HDRP" ? "HDRP/Unlit" : "Unlit/Color";
        string colorProp = (pipelineType == "URP" || pipelineType == "HDRP") ? "_BaseColor" : "_Color";
        string texProp = pipelineType == "URP" ? "_BaseMap"
            : pipelineType == "HDRP" ? "_BaseColorMap" : "_MainTex";

        string[] shadersToCheck = pipelineType == "URP"
            ? new[] { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Universal Render Pipeline/Unlit", "Universal Render Pipeline/Particles/Lit", "Universal Render Pipeline/Particles/Unlit" }
            : pipelineType == "HDRP"
            ? new[] { "HDRP/Lit", "HDRP/Unlit", "HDRP/LitTessellation" }
            : new[] { "Standard", "Standard (Specular setup)", "Unlit/Color", "Unlit/Texture", "Mobile/Diffuse" };

        var availableShaders = new List<object>();
        foreach (var s in shadersToCheck)
            availableShaders.Add(new { name = s, available = Shader.Find(s) != null });

        result.SetResult(new
        {
            success = true,
            pipelineType,
            pipelineName = currentRP != null ? currentRP.name : "Built-in Render Pipeline",
            defaultShader,
            unlitShader,
            colorProperty = colorProp,
            textureProperty = texProp,
            availableShaders
        });
    }
}
```
