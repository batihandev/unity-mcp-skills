# console_get_stats

Get log statistics (count by type) from the Unity console.

**Signature:** `ConsoleGetStats()` — no parameters.

**Returns:** `{ success, total, source, logs, warnings, errors }`

## Notes

- Uses `LogEntries.GetCountsByType` via reflection (public static, no BindingFlags needed).

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var logEntriesType = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
        if (logEntriesType == null)
            { result.SetResult(new { error = "LogEntries API not found" }); return; }

        System.Reflection.MethodInfo getCountsMethod = null;
        foreach (var m in logEntriesType.GetMethods())
        {
            if (m.IsStatic && m.Name == "GetCountsByType") { getCountsMethod = m; break; }
        }
        if (getCountsMethod == null)
            { result.SetResult(new { error = "GetCountsByType not found" }); return; }

        var args = new object[] { 0, 0, 0 };
        getCountsMethod.Invoke(null, args);
        int errCount  = (int)args[0];
        int warnCount = (int)args[1];
        int logCount  = (int)args[2];

        result.SetResult(new {
            success = true,
            total = errCount + warnCount + logCount,
            source = "console",
            logs = logCount,
            warnings = warnCount,
            errors = errCount
        });
    }
}
```
