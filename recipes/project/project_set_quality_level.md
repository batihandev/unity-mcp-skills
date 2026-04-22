# project_set_quality_level

Switch the active quality level by index or by name. Mutates the runtime quality setting (does not persist across Play mode exits unless saved).

**Signature:** `ProjectSetQualityLevel(int level = -1, string levelName = null)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `level` | int | No | -1 | Quality level index (0-based) |
| `levelName` | string | No | null | Quality level name (e.g. `"High"`) |

At least one of `level` or `levelName` must be provided and valid.

## Returns

```json
{ "success": true, "level": 2, "name": "High" }
```

On invalid name:
```json
{ "error": "Quality level 'Ultra' not found" }
```

On out-of-range index:
```json
{ "error": "Invalid quality level: 99" }
```

## Notes

- When `levelName` is provided it takes precedence: the matching index is resolved first.
- Quality level names are defined in Project Settings > Quality. Use `project_get_quality_settings` to list them.
- `QualitySettings.SetQualityLevel(level, true)` applies quality settings immediately.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int level = -1;          // set to target index, or leave -1 when using levelName
        string levelName = null; // set to target name, or leave null when using level index

        if (!string.IsNullOrEmpty(levelName))
        {
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == levelName) { level = i; break; }
            }
            if (level < 0)
            {
                result.SetResult(new { error = $"Quality level '{levelName}' not found" });
                return;
            }
        }

        if (level < 0 || level >= QualitySettings.names.Length)
        {
            result.SetResult(new { error = $"Invalid quality level: {level}" });
            return;
        }

        QualitySettings.SetQualityLevel(level, true);
        result.SetResult(new { success = true, level, name = QualitySettings.names[level] });
    }
}
```
