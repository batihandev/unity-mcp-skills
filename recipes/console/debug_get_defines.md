# debug_get_defines

Get the scripting define symbols (preprocessor symbols) configured for the currently selected build target group.

**Signature:** `DebugGetDefines()` — no parameters.

**Returns:** `{ success, buildTargetGroup, defines }` — `defines` is a semicolon-separated string.

## Notes

- Uses `EditorUserBuildSettings.selectedBuildTargetGroup` to resolve the target.
- Use `debug_set_defines` to modify the symbols.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var group = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        result.SetResult(new { success = true, buildTargetGroup = group.ToString(), defines });
    }
}
```
