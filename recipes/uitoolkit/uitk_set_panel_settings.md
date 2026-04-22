# uitk_set_panel_settings

Selectively update properties on an existing PanelSettings asset.

**Signature:** `UitkSetPanelSettings(assetPath string, scaleMode string = null, referenceResolutionX int? = null, referenceResolutionY int? = null, screenMatchMode string = null, themeStyleSheetPath string = null, textSettingsPath string = null, targetTexturePath string = null, targetDisplay int? = null, sortOrder float? = null, scale float? = null, match float? = null, referenceDpi float? = null, fallbackDpi float? = null, referenceSpritePixelsPerUnit float? = null, dynamicAtlasMinSize int? = null, dynamicAtlasMaxSize int? = null, dynamicAtlasMaxSubTextureSize int? = null, dynamicAtlasFilters string = null, clearColor bool? = null, colorClearR float? = null, colorClearG float? = null, colorClearB float? = null, colorClearA float? = null, clearDepthStencil bool? = null, renderMode string = null, forceGammaRendering bool? = null, bindingLogLevel string = null, colliderUpdateMode string = null, colliderIsTrigger bool? = null, vertexBudget int? = null, textureSlotCount int? = null)`

**Returns:** `{ success, path, scaleMode, referenceResolution, screenMatchMode }`

**Notes:**
- Only provided (non-null) parameters are changed; all others are left as-is.
- `renderMode`, `colliderUpdateMode`, and `colliderIsTrigger` are Unity 6+ internal fields updated via `SerializedObject`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/UI/MyPanelSettings.asset";
        string scaleMode = null;
        int? referenceResolutionX = null;
        int? referenceResolutionY = null;
        string screenMatchMode = null;
        string themeStyleSheetPath = null;

        if (Validate.SafePath(assetPath, "assetPath") is object pathErr) { result.SetResult(pathErr); return; }
        var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(assetPath);
        if (settings == null) { result.SetResult(new { error = $"PanelSettings not found: {assetPath}" }); return; }

        Undo.RecordObject(settings, "Set PanelSettings");

        if (!string.IsNullOrEmpty(scaleMode) && System.Enum.TryParse<PanelScaleMode>(scaleMode, true, out var parsedScale))
            settings.scaleMode = parsedScale;

        if (referenceResolutionX.HasValue || referenceResolutionY.HasValue)
        {
            var cur = settings.referenceResolution;
            settings.referenceResolution = new Vector2Int(
                referenceResolutionX ?? cur.x,
                referenceResolutionY ?? cur.y);
        }

        if (!string.IsNullOrEmpty(screenMatchMode) && System.Enum.TryParse<PanelScreenMatchMode>(screenMatchMode, true, out var parsedMatch))
            settings.screenMatchMode = parsedMatch;

        if (!string.IsNullOrEmpty(themeStyleSheetPath))
        {
            if (Validate.SafePath(themeStyleSheetPath, "themeStyleSheetPath") is object tssErr) { result.SetResult(tssErr); return; }
            var tss = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themeStyleSheetPath);
            if (tss == null) { result.SetResult(new { error = $"ThemeStyleSheet not found: {themeStyleSheetPath}" }); return; }
            settings.themeStyleSheet = tss;
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        WorkflowManager.SnapshotObject(settings);

        result.SetResult(new
        {
            success = true,
            path = assetPath,
            scaleMode = settings.scaleMode.ToString(),
            referenceResolution = $"{settings.referenceResolution.x}x{settings.referenceResolution.y}",
            screenMatchMode = settings.screenMatchMode.ToString()
        });
    }
}
```
