# console_set_pause_on_error

Enable or disable the Error Pause setting in Play mode (equivalent to the pause button in the console toolbar).

**Signature:** `ConsoleSetPauseOnError(bool enabled = true)`

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `enabled` | bool | No | `true` | `true` to enable Error Pause, `false` to disable |

**Returns:** `{ success, enabled }`

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

        // s_ConsoleFlags is private static — inaccessible without BindingFlags in this context
        EditorPrefs.SetBool("DeveloperMode_ErrorPause", enabled);
        result.SetResult(new { success = true, enabled });
    }
}
```
