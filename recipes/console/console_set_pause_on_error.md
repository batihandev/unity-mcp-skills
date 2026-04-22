# console_set_pause_on_error

Enable or disable the Error Pause setting in Play mode (equivalent to the pause button in the console toolbar).

**Signature:** `ConsoleSetPauseOnError(bool enabled = true)`

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `enabled` | bool | No | `true` | `true` to enable Error Pause, `false` to disable |

**Returns:** `{ success, enabled }` — or `{ success, enabled, note }` if the fallback EditorPrefs path was used.

## Notes

- Sets console flag bit `256` in `s_ConsoleFlags` via reflection.
- Falls back to `EditorPrefs.SetBool("DeveloperMode_ErrorPause", enabled)` if the internal field is not found (e.g., future Unity versions).

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool enabled = true;

        var consoleType = System.Type.GetType("UnityEditor.ConsoleWindow, UnityEditor");
        if (consoleType == null)
        {
            result.SetResult(new { error = "ConsoleWindow not found" });
            return;
        }

        var flagField = consoleType.GetField("s_ConsoleFlags",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (flagField == null)
        {
            EditorPrefs.SetBool("DeveloperMode_ErrorPause", enabled);
            result.SetResult(new { success = true, enabled, note = "Set via EditorPrefs" });
            return;
        }

        int flags = (int)flagField.GetValue(null);
        flags = enabled ? flags | 256 : flags & ~256;
        flagField.SetValue(null, flags);
        result.SetResult(new { success = true, enabled });
    }
}
```
