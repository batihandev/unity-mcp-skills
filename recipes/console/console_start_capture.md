# console_start_capture

Mark the start of a console capture session. Sets an EditorPrefs timestamp that `console_get_stats` and `console_export` can use as a reference point.

**Signature:** `ConsoleStartCapture()` — no parameters.

**Returns:** `{ success, message }`

## Notes

- Unity_RunCommand is stateless between calls; this sets an EditorPrefs marker only.
- `console_get_stats` and `console_export` always read from the live Unity console.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        EditorPrefs.SetBool("UnitySkills_Capture_Active", true);
        EditorPrefs.SetString("UnitySkills_Capture_StartTime", System.DateTime.UtcNow.ToString("o"));
        result.SetResult(new { success = true, message = "Capture session started" });
    }
}
```
