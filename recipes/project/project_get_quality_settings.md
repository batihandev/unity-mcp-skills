# project_get_quality_settings

Get the current quality level and all quality level definitions, plus key rendering quality parameters. Read-only; no parameters required.

**Signature:** `ProjectGetQualitySettings()`

**Returns:** `{ success, currentLevel, currentName, allLevels: [{ index, name }], shadows, shadowResolution, antiAliasing, vSyncCount, lodBias, maximumLODLevel }`

## Notes

- Quality settings are read-only here. To change the active level use `project_set_quality_level`.
- `allLevels` contains all levels defined in Project Settings > Quality, ordered by index.
- `shadows`, `shadowResolution` are string representations of the corresponding `ShadowQuality` and `ShadowResolution` enums.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

## C# Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var qualityNames = QualitySettings.names;
        var currentLevel = QualitySettings.GetQualityLevel();

        result.SetResult(new
        {
            success = true,
            currentLevel,
            currentName = qualityNames[currentLevel],
            allLevels = qualityNames.Select((name, index) => new { index, name }).ToList(),
            shadows = QualitySettings.shadows.ToString(),
            shadowResolution = QualitySettings.shadowResolution.ToString(),
            antiAliasing = QualitySettings.antiAliasing,
            vSyncCount = QualitySettings.vSyncCount,
            lodBias = QualitySettings.lodBias,
            maximumLODLevel = QualitySettings.maximumLODLevel
        });
    }
}
```
