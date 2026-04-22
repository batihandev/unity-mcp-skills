# _shared/project_skills

Render-pipeline detection + pipeline-specific shader / property name helpers. Used by `material_*` and `perception/project_stack_detect`.

## When to declare

Recipe references `ProjectSkills.DetectRenderPipeline`, `GetDefaultShaderName`, `GetUnlitShaderName`, `GetColorPropertyName`, or `GetMainTexturePropertyName`.

## Paste-in

```csharp
internal static class ProjectSkills
{
    public enum RenderPipelineType { BuiltIn, URP, HDRP, Custom }

    public static RenderPipelineType DetectRenderPipeline()
    {
        var currentRP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        if (currentRP == null) return RenderPipelineType.BuiltIn;
        var name = currentRP.GetType().Name;
        if (name.Contains("Universal") || name.Contains("URP")) return RenderPipelineType.URP;
        if (name.Contains("HDRender") || name.Contains("HDRP")) return RenderPipelineType.HDRP;
        return RenderPipelineType.Custom;
    }

    public static string GetDefaultShaderName()
    {
        switch (DetectRenderPipeline())
        {
            case RenderPipelineType.URP: return "Universal Render Pipeline/Lit";
            case RenderPipelineType.HDRP: return "HDRP/Lit";
            default: return "Standard";
        }
    }

    public static string GetUnlitShaderName()
    {
        switch (DetectRenderPipeline())
        {
            case RenderPipelineType.URP: return "Universal Render Pipeline/Unlit";
            case RenderPipelineType.HDRP: return "HDRP/Unlit";
            default: return "Unlit/Color";
        }
    }

    public static string GetColorPropertyName()
    {
        var p = DetectRenderPipeline();
        return (p == RenderPipelineType.URP || p == RenderPipelineType.HDRP) ? "_BaseColor" : "_Color";
    }

    public static string GetMainTexturePropertyName()
    {
        switch (DetectRenderPipeline())
        {
            case RenderPipelineType.URP: return "_BaseMap";
            case RenderPipelineType.HDRP: return "_BaseColorMap";
            default: return "_MainTex";
        }
    }
}
```
