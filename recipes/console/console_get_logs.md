# console_get_logs

Get Unity console logs. Works in two modes:

- **Direct mode** (default): reads existing entries from Unity Console history via `LogEntries` reflection — no setup needed.
- **Capture mode**: when `console_start_capture` is active, returns buffered entries with precise timestamps.

**Signature:** `ConsoleGetLogs(string type = "All", string filter = null, int limit = 100)`

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `type` | string | No | `"All"` | `All`, `Error`, `Warning`, or `Log` |
| `filter` | string | No | null | Substring filter applied to message text |
| `limit` | int | No | `100` | Maximum entries to return |

**Returns:** `{ count, logs: [{type, message, time?}], source }` — `source` is `"capture"` or `"console"`.

## Common Mistakes

- `console_filter` does not exist — use `console_get_logs` with the `filter` parameter.
- `console_read` does not exist — use `console_get_logs`.
- Do not confuse with `debug_get_logs`: `console_get_logs` reads the captured buffer (with timestamps), while `debug_get_logs` always reads console history filtered to errors/warnings by default.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string type = "All";   // All | Error | Warning | Log
        string filter = null;  // optional substring filter
        int limit = 100;

        if (_capturing)
        {
            lock (_logLock)
            {
                IEnumerable<LogEntry> results = _logs;
                if (type != "All")
                    results = results.Where(l => CapturedLogMatchesType(l.type, type));
                if (!string.IsNullOrEmpty(filter))
                    results = results.Where(l => l.message.Contains(filter));

                var captured = results.TakeLast(limit).Select(l => new
                {
                    type = l.type.ToString(),
                    message = l.message,
                    time = l.time.ToString("HH:mm:ss.fff")
                }).ToArray();
                result.Return(new { count = captured.Length, logs = captured, source = "capture" });
                return;
            }
        }

        // Direct mode: read from Unity Console via LogEntries reflection
        int targetMask = 0;
        if (type == "All" || type.Contains("Error"))   targetMask |= DebugSkills.ErrorModeMask;
        if (type == "All" || type.Contains("Warning")) targetMask |= DebugSkills.WarningModeMask;
        if (type == "All" || type.Contains("Log"))     targetMask |= DebugSkills.LogModeMask;
        if (targetMask == 0) targetMask = DebugSkills.ErrorModeMask | DebugSkills.WarningModeMask | DebugSkills.LogModeMask;

        var logs = DebugSkills.ReadLogEntries(targetMask, filter, limit);
        result.Return(new { count = logs.Count, logs, source = "console" });
    }
}
```
