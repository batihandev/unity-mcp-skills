# project_get_player_settings

Get Player Settings including product name, company name, bundle version, default resolution, fullscreen mode, API compatibility level, and scripting backend. Read-only; no parameters required.

**Signature:** `ProjectGetPlayerSettings()`

**Returns:** `{ success, productName, companyName, bundleVersion, defaultScreenWidth, defaultScreenHeight, fullscreen, apiCompatibility, scriptingBackend }`

## Notes

- Player Settings are read-only via this command. To edit them, open Project Settings via `editor_execute_menu` with `menuPath="Edit/Project Settings..."` and navigate to Player.
- `apiCompatibility` and `scriptingBackend` reflect the currently selected build target group.
- `fullscreen` is a string representation of the `FullScreenMode` enum (e.g. `"ExclusiveFullScreen"`, `"Windowed"`).

## C# Template

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
            productName = PlayerSettings.productName,
            companyName = PlayerSettings.companyName,
            bundleVersion = PlayerSettings.bundleVersion,
            defaultScreenWidth = PlayerSettings.defaultScreenWidth,
            defaultScreenHeight = PlayerSettings.defaultScreenHeight,
            fullscreen = PlayerSettings.fullScreenMode.ToString(),
            apiCompatibility = PlayerSettings.GetApiCompatibilityLevel(
                EditorUserBuildSettings.selectedBuildTargetGroup).ToString(),
            scriptingBackend = PlayerSettings.GetScriptingBackend(
                EditorUserBuildSettings.selectedBuildTargetGroup).ToString()
        });
    }
}
```
