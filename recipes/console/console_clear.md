# console_clear

Clear the Unity console and the in-memory capture buffer.

**Signature:** `ConsoleClear()` — no parameters.

**Returns:** `{ success, message }`

## Notes

- Uses `UnityEditor.LogEntries.Clear()` via reflection to clear the console window.
- Also clears the internal capture buffer (`_logs`).

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
        var logEntries = assembly.GetType("UnityEditor.LogEntries");
        var clearMethod = logEntries.GetMethod("Clear");
        clearMethod.Invoke(null, null);

        lock (_logLock) { _logs.Clear(); }
        result.Return(new { success = true, message = "Console cleared" });
    }
}
```
