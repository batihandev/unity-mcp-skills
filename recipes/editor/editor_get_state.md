# editor_get_state

Get the current editor state including play/pause/compile flags, Unity version, and build platform.

**Signature:** `EditorGetState()`

**Returns:** `{ isPlaying, isPaused, isCompiling, timeSinceStartup, unityVersion, platform }`

Note: no top-level `success` key. The SKILL.md description omitted `timeSinceStartup` and `unityVersion` — both are present in the upstream implementation.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        result.SetResult(new
        {
            isPlaying        = EditorApplication.isPlaying,
            isPaused         = EditorApplication.isPaused,
            isCompiling      = EditorApplication.isCompiling,
            timeSinceStartup = EditorApplication.timeSinceStartup,
            unityVersion     = Application.unityVersion,
            platform         = Application.platform.ToString()
        });
    }
}
```
