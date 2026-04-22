# editor_play

Enter play mode. Fire-and-forget: sets `EditorApplication.isPlaying = true` and
returns immediately. Observe the transition by polling `editor_get_state`
(`isPlaying` field) in a later call.

**Signature:** `EditorPlay()`

**Returns:** `{ success, mode, started }` on success; `{ error }` if already
in play mode.

**Risk:** medium — unsaved scene changes made during play mode are lost on
exit.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code
block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if (EditorApplication.isPlaying)
        {
            result.SetResult(new { error = "Already in play mode" });
            return;
        }

        EditorApplication.isPlaying = true;
        result.SetResult(new { success = true, mode = "entering_play_mode", started = true });
    }
}
```
