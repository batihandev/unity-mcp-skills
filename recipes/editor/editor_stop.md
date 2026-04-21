# editor_stop

Exit play mode. Any scene changes made during play mode are lost.

**Signature:** `EditorStop()`

**Returns:** `{ success, mode }` on success; `{ error }` if not in play mode.

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
        if (!EditorApplication.isPlaying)
        {
            result.SetResult(new { error = "Not in play mode" });
            return;
        }

        EditorApplication.isPlaying = false;
        result.SetResult(new { success = true, mode = "stopped" });
    }
}
```
