# console_set_clear_on_play

Enable or disable the Clear on Play setting in the Unity console (console is cleared automatically when entering Play mode).

**Signature:** `ConsoleSetClearOnPlay(bool enabled)`

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `enabled` | bool | Yes | — | `true` to enable Clear on Play, `false` to disable |

**Returns:** `{ success, setting, enabled }` — or `{ success, setting, enabled, note }` on EditorPrefs fallback.

## Notes

- Attempts `SetConsoleFlag` (Unity 6+ public static method) first; falls back to `EditorPrefs`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool enabled = true;
        result.SetResult(SetConsoleFlag(16, enabled, "ClearOnPlay"));
    }

    private static object SetConsoleFlag(int flag, bool enabled, string name)
    {
        var consoleType = System.Type.GetType("UnityEditor.ConsoleWindow, UnityEditor");
        if (consoleType == null) return new { error = "ConsoleWindow not found" };

        System.Reflection.MethodInfo setFlagMethod = null;
        foreach (var m in consoleType.GetMethods())
        {
            if (m.IsStatic && m.Name == "SetConsoleFlag") { setFlagMethod = m; break; }
        }
        if (setFlagMethod != null)
        {
            try
            {
                setFlagMethod.Invoke(null, new object[] { flag, enabled });
                return new { success = true, setting = name, enabled };
            }
            catch { }
        }

        EditorPrefs.SetBool("UnitySkills_Console_" + name, enabled);
        return new { success = true, setting = name, enabled, note = "Set via EditorPrefs fallback" };
    }
}
```
