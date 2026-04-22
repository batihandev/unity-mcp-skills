# console_start_capture

Start capturing Unity console logs into an in-memory buffer. Once active, `console_get_logs` returns buffered entries with timestamps rather than reading the raw console history.

**Signature:** `ConsoleStartCapture()` — no parameters.

**Returns:** `{ success, message }`

## Notes

- The buffer is cleared on each call to `console_start_capture`.
- Buffer capacity: 1000 entries (oldest are dropped when exceeded).
- Call `console_stop_capture` when done to detach the log listener.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if (!_capturing)
        {
            Application.logMessageReceived += OnLogMessage;
            _capturing = true;
        }
        lock (_logLock) { _logs.Clear(); }
        result.SetResult(new { success = true, message = "Console capture started" });
    }
}
```
