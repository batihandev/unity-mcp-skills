# project_get_layers

Get all layer definitions from the project's TagManager. Read-only; no parameters required.

**Signature:** `ProjectGetLayers()`

**Returns:** `{ success, count, layers: string[] }`

## Notes

- Layers are read-only via this command. To add or rename layers, open Project Settings via `editor_execute_menu` with `menuPath="Edit/Project Settings..."` then navigate to Tags and Layers.
- The returned array contains only non-empty layer names (Unity skips empty slots).
- Layer indices are not returned; use `LayerMask.NameToLayer(name)` in code to get them.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var layers = UnityEditorInternal.InternalEditorUtility.layers;
        result.SetResult(new { success = true, count = layers.Length, layers });
    }
}
```
