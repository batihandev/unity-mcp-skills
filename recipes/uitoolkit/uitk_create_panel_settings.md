# uitk_create_panel_settings

Create a new PanelSettings asset for UI Toolkit rendering.

**Signature:** `UitkCreatePanelSettings(savePath string, scaleMode string = "ScaleWithScreenSize", referenceResolutionX int = 1920, referenceResolutionY int = 1080, screenMatchMode string = "MatchWidthOrHeight", themeStyleSheetPath string = null, textSettingsPath string = null, targetTexturePath string = null, targetDisplay int? = null, sortOrder float? = null, scale float? = null, match float? = null, referenceDpi float? = null, fallbackDpi float? = null, referenceSpritePixelsPerUnit float? = null, dynamicAtlasMinSize int? = null, dynamicAtlasMaxSize int? = null, dynamicAtlasMaxSubTextureSize int? = null, dynamicAtlasFilters string = null, clearColor bool? = null, colorClearR float? = null, colorClearG float? = null, colorClearB float? = null, colorClearA float? = null, clearDepthStencil bool? = null, renderMode string = null, forceGammaRendering bool? = null, bindingLogLevel string = null, colliderUpdateMode string = null, colliderIsTrigger bool? = null, vertexBudget int? = null, textureSlotCount int? = null)`

**Returns:** `{ success, path, scaleMode, referenceResolution, screenMatchMode }`

**Notes:**
- Fails if the file already exists.
- `scaleMode`: `ScaleWithScreenSize` (default), `ConstantPixelSize`, or `ConstantPhysicalSize`.
- `renderMode`, `colliderUpdateMode`, and `colliderIsTrigger` are Unity 6+ only and are applied via `SerializedObject`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string savePath = "Assets/UI/MyPanelSettings.asset";
        string scaleMode = "ScaleWithScreenSize";
        int referenceResolutionX = 1920;
        int referenceResolutionY = 1080;
        string screenMatchMode = "MatchWidthOrHeight";
        string themeStyleSheetPath = null;

        if (Validate.SafePath(savePath, "savePath") is object pathErr) { result.SetResult(pathErr); return; }
        if (File.Exists(savePath)) { result.SetResult(new { error = $"File already exists: {savePath}" }); return; }

        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var settings = ScriptableObject.CreateInstance<PanelSettings>();

        if (System.Enum.TryParse<PanelScaleMode>(scaleMode, true, out var parsedScale))
            settings.scaleMode = parsedScale;

        settings.referenceResolution = new Vector2Int(referenceResolutionX, referenceResolutionY);

        if (System.Enum.TryParse<PanelScreenMatchMode>(screenMatchMode, true, out var parsedMatch))
            settings.screenMatchMode = parsedMatch;

        if (!string.IsNullOrEmpty(themeStyleSheetPath))
        {
            if (Validate.SafePath(themeStyleSheetPath, "themeStyleSheetPath") is object tssErr) { result.SetResult(tssErr); return; }
            var tss = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themeStyleSheetPath);
            if (tss != null) settings.themeStyleSheet = tss;
        }

        AssetDatabase.CreateAsset(settings, savePath);
        AssetDatabase.SaveAssets();
        WorkflowManager.SnapshotObject(settings, SnapshotType.Created);

        result.SetResult(new
        {
            success = true,
            path = savePath,
            scaleMode = settings.scaleMode.ToString(),
            referenceResolution = $"{referenceResolutionX}x{referenceResolutionY}",
            screenMatchMode = settings.screenMatchMode.ToString()
        });
    }
}
```
