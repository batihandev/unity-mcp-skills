# uitk_get_panel_settings

Read all properties from an existing PanelSettings asset.

**Signature:** `UitkGetPanelSettings(assetPath string)`

**Returns:** `{ path, scaleMode, referenceResolution, screenMatchMode, themeStyleSheet, textSettings, targetTexture, targetDisplay, sortingOrder, scale, match, referenceDpi, fallbackDpi, referenceSpritePixelsPerUnit, dynamicAtlasSettings, clearColor, colorClearValue, clearDepthStencil, renderMode, forceGammaRendering, bindingLogLevel, colliderUpdateMode, colliderIsTrigger, vertexBudget, textureSlotCount }`

**Notes:**
- `renderMode`, `colliderUpdateMode`, and `colliderIsTrigger` are internal fields read via `SerializedObject`.
- `textureSlotCount` requires Unity 6000.3+; earlier 6000.x returns only `vertexBudget`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/UI/MyPanelSettings.asset";

        if (Validate.SafePath(assetPath, "assetPath") is object pathErr) { result.SetResult(pathErr); return; }
        var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(assetPath);
        if (settings == null) { result.SetResult(new { error = $"PanelSettings not found: {assetPath}" }); return; }

        var atlas = settings.dynamicAtlasSettings;
        var cc = settings.colorClearValue;

        var so = new SerializedObject(settings);
        var rmProp = so.FindProperty("m_RenderMode");
        int rmVal = rmProp != null ? rmProp.intValue : 0;
        string renderModeStr = rmVal == 1 ? "WorldSpace" : "ScreenSpaceOverlay";
        var cuProp = so.FindProperty("m_ColliderUpdateMode");
        int cuVal = cuProp != null ? cuProp.intValue : 0;
        string colliderUpdateStr = cuVal == 2 ? "KeepExistingCollider" : cuVal == 1 ? "Match2DDocumentRect" : "Match3DBoundingBox";
        var ctProp = so.FindProperty("m_ColliderIsTrigger");
        bool colliderIsTriggerVal = ctProp != null ? ctProp.boolValue : true;

        result.SetResult(new
        {
            path = assetPath,
            scaleMode = settings.scaleMode.ToString(),
            referenceResolution = new { x = settings.referenceResolution.x, y = settings.referenceResolution.y },
            screenMatchMode = settings.screenMatchMode.ToString(),
            themeStyleSheet = settings.themeStyleSheet != null ? AssetDatabase.GetAssetPath(settings.themeStyleSheet) : null,
            textSettings    = settings.textSettings    != null ? AssetDatabase.GetAssetPath(settings.textSettings)    : null,
            targetTexture   = settings.targetTexture   != null ? AssetDatabase.GetAssetPath(settings.targetTexture)   : null,
            targetDisplay   = settings.targetDisplay,
            sortingOrder    = settings.sortingOrder,
            scale           = settings.scale,
            match           = settings.match,
            referenceDpi    = settings.referenceDpi,
            fallbackDpi     = settings.fallbackDpi,
            referenceSpritePixelsPerUnit = settings.referenceSpritePixelsPerUnit,
            dynamicAtlasSettings = new
            {
                minAtlasSize      = atlas.minAtlasSize,
                maxAtlasSize      = atlas.maxAtlasSize,
                maxSubTextureSize = atlas.maxSubTextureSize,
                activeFilters     = atlas.activeFilters.ToString()
            },
            clearColor          = settings.clearColor,
            colorClearValue     = new { r = cc.r, g = cc.g, b = cc.b, a = cc.a },
            clearDepthStencil   = settings.clearDepthStencil,
            renderMode          = renderModeStr,
            forceGammaRendering = settings.forceGammaRendering,
            bindingLogLevel     = settings.bindingLogLevel.ToString(),
            colliderUpdateMode  = colliderUpdateStr,
            colliderIsTrigger   = colliderIsTriggerVal,
            vertexBudget        = settings.vertexBudget
        });
    }
}
```
