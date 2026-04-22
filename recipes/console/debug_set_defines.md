# debug_set_defines

Set the scripting define symbols (preprocessor symbols) for the currently selected build target group.

**Signature:** `DebugSetDefines(string defines)`

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `defines` | string | Yes | — | Semicolon-separated list of define symbols, e.g. `"MY_DEFINE;ANOTHER_DEFINE"` |

**Returns:** `{ success, buildTargetGroup, defines }`

## Notes

- Changing defines triggers immediate recompilation; subsequent `Unity_RunCommand` calls queue during the reload window.
- To read current defines first, call `debug_get_defines`.
- Replaces all existing defines — pass the full desired set, not a diff.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string defines = "MY_DEFINE;ANOTHER_DEFINE";

        var group = EditorUserBuildSettings.selectedBuildTargetGroup;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);

        result.SetResult(new { success = true, buildTargetGroup = group.ToString(), defines });
    }
}
```
