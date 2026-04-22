# editor_pause

Toggle the pause state of play mode.

**Signature:** `EditorPause()`

**Returns:** `{ success, paused }` — `paused` reflects the new state after the toggle.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        EditorApplication.isPaused = !EditorApplication.isPaused;
        result.SetResult(new { success = true, paused = EditorApplication.isPaused });
    }
}
```
