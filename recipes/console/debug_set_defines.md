# debug_set_defines

Set the scripting define symbols (preprocessor symbols) for the currently selected build target group.

**Signature:** `DebugSetDefines(string defines)`

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `defines` | string | Yes | — | Semicolon-separated list of define symbols, e.g. `"MY_DEFINE;ANOTHER_DEFINE"` |

**Returns:** `{ success, buildTargetGroup, defines, serverAvailability }`

## Notes

- Changing defines triggers immediate recompilation; the REST server will be transiently unavailable.
- `serverAvailability` contains a structured notice; always surface it to the user.
- To read current defines first, call `debug_get_defines`.
- Replaces all existing defines — pass the full desired set, not a diff.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string defines = "MY_DEFINE;ANOTHER_DEFINE";  // full semicolon-separated list

        var group = EditorUserBuildSettings.selectedBuildTargetGroup;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
        result.Return(new
        {
            success = true,
            buildTargetGroup = group.ToString(),
            defines,
            serverAvailability = ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                "Scripting define symbols changed. Unity may recompile assemblies immediately.",
                alwaysInclude: true)
        });
    }
}
```
