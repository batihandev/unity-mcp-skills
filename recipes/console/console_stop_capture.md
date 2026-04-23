# console_stop_capture

Clear the console capture session marker set by `console_start_capture`.

**Signature:** `ConsoleStopCapture()` — no parameters.

**Returns:** `{ success, message }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        EditorPrefs.SetBool("UnitySkills_Capture_Active", false);
        EditorPrefs.DeleteKey("UnitySkills_Capture_StartTime");
        result.SetResult(new { success = true, message = "Capture session stopped" });
    }
}
```
