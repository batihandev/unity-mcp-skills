# console_export

Export console logs to a file. Uses the in-memory capture buffer when `console_start_capture` is active; otherwise reads directly from Unity Console history.

**Signature:** `ConsoleExport(string savePath = "Assets/console_log.txt")`

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `savePath` | string | No | `"Assets/console_log.txt"` | File path to write; parent directory is created if missing |

**Returns:** `{ success, path, count, source }` — `source` is `"capture"` or `"console"`.

## Notes

- Path is validated by `Validate.SafePath` to prevent directory traversal.
- Capture mode format: `[HH:mm:ss.fff] [LogType] message`
- Direct mode format: `[LogType] message`
- Direct mode reads up to 1000 entries.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string savePath = "Assets/console_log.txt";

        if (Validate.SafePath(savePath, "savePath") is object pathErr)
        {
            result.Return(pathErr);
            return;
        }

        var dir = System.IO.Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        if (_capturing || _logs.Count > 0)
        {
            lock (_logLock)
            {
                var lines = _logs.Select(l => $"[{l.time:HH:mm:ss.fff}] [{l.type}] {l.message}");
                System.IO.File.WriteAllLines(savePath, lines);
                result.Return(new { success = true, path = savePath, count = _logs.Count, source = "capture" });
                return;
            }
        }

        // Direct mode: read from Unity Console when no capture buffer is available
        int allMask = DebugSkills.ErrorModeMask | DebugSkills.WarningModeMask | DebugSkills.LogModeMask;
        var entries = DebugSkills.ReadLogEntries(allMask, null, 1000);
        var directLines = entries.Select(e => { dynamic d = e; return $"[{d.type}] {d.message}"; });
        System.IO.File.WriteAllLines(savePath, directLines.Cast<string>());
        result.Return(new { success = true, path = savePath, count = entries.Count, source = "console" });
    }
}
```
