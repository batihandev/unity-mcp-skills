# console_export

Export console logs to a file by reading directly from Unity's LogEntries.

**Signature:** `ConsoleExport(string savePath = "Assets/console_log.txt")`

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `savePath` | string | No | `"Assets/console_log.txt"` | File path to write; parent directory is created if missing |

**Returns:** `{ success, path, count, source }`

## Notes

- Reads up to 1000 entries from the Unity console via `StartGettingEntries`/`GetEntryInternal`.
- Mode bit interpretation: bit 64 = Warning; bits 1/2/256 = Error; otherwise Log.
- Path is validated by `Validate.SafePath` to prevent directory traversal.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md)

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
            { result.SetResult(pathErr); return; }

        var dir = System.IO.Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        var logEntriesType = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
        var logEntryType   = System.Type.GetType("UnityEditor.LogEntry, UnityEditor");
        if (logEntriesType == null || logEntryType == null)
            { result.SetResult(new { error = "LogEntries API not found" }); return; }

        System.Reflection.MethodInfo startMethod = null, endMethod = null, getEntryMethod = null;
        foreach (var m in logEntriesType.GetMethods())
        {
            if (!m.IsStatic) continue;
            if (m.Name == "StartGettingEntries") startMethod = m;
            else if (m.Name == "EndGettingEntries") endMethod = m;
            else if (m.Name == "GetEntryInternal") getEntryMethod = m;
        }
        if (startMethod == null || endMethod == null || getEntryMethod == null)
            { result.SetResult(new { error = "Required LogEntries methods not found" }); return; }

        var msgField  = logEntryType.GetField("message");
        var modeField = logEntryType.GetField("mode");

        int totalCount = (int)startMethod.Invoke(null, null);
        int readCount  = System.Math.Min(totalCount, 1000);
        var lines = new System.Collections.Generic.List<string>(readCount);
        try
        {
            for (int i = 0; i < readCount; i++)
            {
                var entryObj = System.Activator.CreateInstance(logEntryType);
                var invokeArgs = new object[] { i, entryObj };
                bool ok = (bool)getEntryMethod.Invoke(null, invokeArgs);
                if (!ok) break;
                entryObj = invokeArgs[1];
                string msg  = msgField  != null ? (string)msgField.GetValue(entryObj)  : "";
                int    mode = modeField != null ? (int)modeField.GetValue(entryObj)    : 0;
                string typeStr = (mode & 64) != 0 ? "Warning" : (mode & (1 | 2 | 256)) != 0 ? "Error" : "Log";
                if (msg != null) msg = msg.Split('\n')[0];
                lines.Add($"[{typeStr}] {msg}");
            }
        }
        finally { endMethod.Invoke(null, null); }

        System.IO.File.WriteAllLines(savePath, lines);
        AssetDatabase.ImportAsset(savePath);
        result.SetResult(new { success = true, path = savePath, count = lines.Count, source = "console" });
    }
}
```
