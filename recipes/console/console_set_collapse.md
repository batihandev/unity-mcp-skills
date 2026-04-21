# console_set_collapse

Enable or disable the Collapse mode in the Unity console (identical repeated messages are folded into one row with a count badge).

**Signature:** `ConsoleSetCollapse(bool enabled)`

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `enabled` | bool | Yes | — | `true` to enable collapse, `false` to disable |

**Returns:** `{ success, setting, enabled }` — or `{ success, setting, enabled, note }` on EditorPrefs fallback.

## Notes

- Sets console flag bit `32` via `SetConsoleFlag` (Unity 6+) or `s_ConsoleFlags` field (legacy).
- Falls back to `EditorPrefs` if reflection fails.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool enabled = true;  // set to desired value
        result.Return(SetConsoleFlag(32, enabled, "Collapse"));
    }

    private static object SetConsoleFlag(int flag, bool enabled, string name)
    {
        var consoleType = System.Type.GetType("UnityEditor.ConsoleWindow, UnityEditor");
        if (consoleType == null) return new { error = "ConsoleWindow not found" };

        // Unity 6+: try SetConsoleFlag method
        var setFlagMethod = consoleType.GetMethod("SetConsoleFlag",
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);
        if (setFlagMethod != null)
        {
            try
            {
                setFlagMethod.Invoke(null, new object[] { flag, enabled });
                return new { success = true, setting = name, enabled };
            }
            catch { /* fall through */ }
        }

        // Legacy: try s_ConsoleFlags field
        var flagField = consoleType.GetField("s_ConsoleFlags",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (flagField != null)
        {
            int flags = (int)flagField.GetValue(null);
            flags = enabled ? flags | flag : flags & ~flag;
            flagField.SetValue(null, flags);
            return new { success = true, setting = name, enabled };
        }

        // Fallback: EditorPrefs
        EditorPrefs.SetBool("UnitySkills_Console_" + name, enabled);
        return new { success = true, setting = name, enabled, note = "Set via EditorPrefs fallback" };
    }
}
```
