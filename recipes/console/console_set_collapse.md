# console_set_collapse

Enable or disable the Collapse mode in the Unity console (identical repeated messages are folded into one row with a count badge).

**Signature:** `ConsoleSetCollapse(bool enabled)`

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `enabled` | bool | Yes | — | `true` to enable collapse, `false` to disable |

**Returns:** `{ success, setting, enabled }` — or `{ success, setting, enabled, note }` on EditorPrefs fallback.

## Notes

- Attempts `SetConsoleFlag` (Unity 6+ public static method) first; falls back to `EditorPrefs`.

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
        result.SetResult(SetConsoleFlag(32, enabled, "Collapse"));
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
