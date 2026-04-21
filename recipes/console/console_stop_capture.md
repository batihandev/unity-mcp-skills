# console_stop_capture

Stop capturing Unity console logs and detach the log listener.

**Signature:** `ConsoleStopCapture()` — no parameters.

**Returns:** `{ success, message, capturedCount }`

## Notes

- Captured logs remain in the buffer after stopping; `console_get_logs` can still read them.
- Call `console_clear` or start a new capture session to discard the buffer.

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
        if (_capturing)
        {
            Application.logMessageReceived -= OnLogMessage;
            _capturing = false;
        }
        int count;
        lock (_logLock) { count = _logs.Count; }
        result.SetResult(new { success = true, message = "Console capture stopped", capturedCount = count });
    }
}
```
