# project_get_build_settings

Get the active build target, build target group, and the list of scenes registered in Build Settings. Read-only; no parameters required.

**Signature:** `ProjectGetBuildSettings()`

**Returns:** `{ success, activeBuildTarget, buildTargetGroup, sceneCount, scenes: [{ index, path, enabled }] }`

## Notes

- Build settings are read-only. To open the Build Settings window use `editor_execute_menu` with `menuPath="File/Build Settings..."`.
- `scenes` lists only scenes added in Build Settings, not all scenes in the project.
- `enabled` reflects whether a scene is checked on in the Build Settings panel.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var scenes = EditorBuildSettings.scenes
            .Select((s, i) => new { index = i, path = s.path, enabled = s.enabled })
            .ToArray();

        result.SetResult(new
        {
            success = true,
            activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
            buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup.ToString(),
            sceneCount = scenes.Length,
            scenes
        });
    }
}
```
