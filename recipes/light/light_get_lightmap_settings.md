# light_get_lightmap_settings

Get the current Lightmap baking settings for the active scene.

**Signature:** `LightGetLightmapSettings()`

**Returns:** `{ success, bakedGI, realtimeGI, lightmapSize, lightmapPadding, isRunning, lightmapCount }`

**Notes:**
- Read-only; no undo entry is created.
- `isRunning` is `true` while a bake is in progress.
- `lightmapSize` corresponds to `LightmapEditorSettings.maxAtlasSize`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        result.SetResult(new
        {
            success = true,
            bakedGI = Lightmapping.bakedGI,
            realtimeGI = Lightmapping.realtimeGI,
            lightmapSize = LightmapEditorSettings.maxAtlasSize,
            lightmapPadding = LightmapEditorSettings.padding,
            isRunning = Lightmapping.isRunning,
            lightmapCount = LightmapSettings.lightmaps.Length
        });
    }
}
```
