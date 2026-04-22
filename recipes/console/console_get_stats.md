# console_get_stats

Get log statistics (count by type). Uses the in-memory capture buffer when `console_start_capture` is active; otherwise reads directly from Unity Console history.

**Signature:** `ConsoleGetStats()` — no parameters.

**Returns:**
- Capture mode: `{ success, total, source="capture", logs, warnings, errors, exceptions, asserts }`
- Direct mode: `{ success, total, source="console", logs, warnings, errors }`

## Notes

- Capture mode differentiates `Exception` and `Assert` types; direct mode maps these to `Error`.
- Direct mode reads up to 10 000 entries to build the count.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if (_capturing || _logs.Count > 0)
        {
            lock (_logLock)
            {
                result.SetResult(new
                {
                    success = true,
                    total = _logs.Count,
                    source = "capture",
                    logs       = _logs.Count(l => l.type == LogType.Log),
                    warnings   = _logs.Count(l => l.type == LogType.Warning),
                    errors     = _logs.Count(l => l.type == LogType.Error),
                    exceptions = _logs.Count(l => l.type == LogType.Exception),
                    asserts    = _logs.Count(l => l.type == LogType.Assert)
                });
                return;
            }
        }

        // Direct mode: read from Unity Console
        int allMask = DebugSkills.ErrorModeMask | DebugSkills.WarningModeMask | DebugSkills.LogModeMask;
        var entries = DebugSkills.ReadLogEntries(allMask, null, 10000);
        int errCount = 0, warnCount = 0, logCount = 0;
        foreach (dynamic e in entries)
        {
            switch ((string)e.type)
            {
                case "Error":   errCount++;  break;
                case "Warning": warnCount++; break;
                default:        logCount++;  break;
            }
        }
        result.SetResult(new
        {
            success = true,
            total = entries.Count,
            source = "console",
            logs = logCount,
            warnings = warnCount,
            errors = errCount
        });
    }
}
```
