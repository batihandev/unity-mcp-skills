# project_get_info

Get project information including Unity version, product name, company name, platform, render pipeline details, and recommended shader names. Read-only; no parameters required.

**Signature:** `ProjectGetInfo()`

**Returns:** `{ success, unityVersion, productName, companyName, platform, renderPipeline: { type, name, assetType }, recommendedShaders: { defaultLit, unlit, colorProperty, mainTextureProperty }, projectPath, isPlaying }`

## Notes

- `renderPipeline.type` is one of `BuiltIn`, `URP`, `HDRP`, or `Custom`.
- `recommendedShaders` provides pipeline-aware shader and property names; use these before hardcoding `"Standard"` or `"_Color"`.
- `isPlaying` reflects `Application.isPlaying` — most edits should only run when `false`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var currentRP = GraphicsSettings.currentRenderPipeline;
        string pipelineType = currentRP == null ? "BuiltIn"
            : currentRP.GetType().Name.Contains("Universal") || currentRP.GetType().Name.Contains("URP") ? "URP"
            : currentRP.GetType().Name.Contains("HDRender") || currentRP.GetType().Name.Contains("HDRP") ? "HDRP"
            : "Custom";

        string defaultLit = pipelineType == "URP" ? "Universal Render Pipeline/Lit"
            : pipelineType == "HDRP" ? "HDRP/Lit" : "Standard";
        string unlit = pipelineType == "URP" ? "Universal Render Pipeline/Unlit"
            : pipelineType == "HDRP" ? "HDRP/Unlit" : "Unlit/Color";
        string colorProp = (pipelineType == "URP" || pipelineType == "HDRP") ? "_BaseColor" : "_Color";
        string texProp = pipelineType == "URP" ? "_BaseMap"
            : pipelineType == "HDRP" ? "_BaseColorMap" : "_MainTex";

        result.SetResult(new
        {
            success = true,
            unityVersion = Application.unityVersion,
            productName = Application.productName,
            companyName = Application.companyName,
            platform = Application.platform.ToString(),
            renderPipeline = new
            {
                type = pipelineType,
                name = currentRP != null ? currentRP.name : "Built-in Render Pipeline",
                assetType = currentRP != null ? (object)currentRP.GetType().Name : null
            },
            recommendedShaders = new
            {
                defaultLit,
                unlit,
                colorProperty = colorProp,
                mainTextureProperty = texProp
            },
            projectPath = Application.dataPath,
            isPlaying = Application.isPlaying
        });
    }
}
```
