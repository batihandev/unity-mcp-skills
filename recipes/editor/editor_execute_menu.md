# editor_execute_menu

Execute a Unity menu item by its exact path. Typos cause silent failure — the menu path must match exactly.

**Signature:** `EditorExecuteMenu(string menuPath)`

**Returns:** `{ success, executed }` on success; `{ error }` if the path was not found or execution failed.

**Common menu paths:**

| Menu Path | Action |
|-----------|--------|
| `File/Save` | Save current scene |
| `File/Build Settings...` | Open Build Settings |
| `Edit/Play` | Toggle play mode |
| `GameObject/Create Empty` | Create empty GameObject |
| `Window/General/Console` | Open Console window |
| `Assets/Refresh` | Refresh asset database |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string menuPath = "File/Save"; // Exact Unity menu path

        var success = EditorApplication.ExecuteMenuItem(menuPath);
        if (!success)
        {
            result.SetResult(new { error = $"Menu item not found or failed: {menuPath}" });
            return;
        }

        result.SetResult(new { success = true, executed = menuPath });
    }
}
```
