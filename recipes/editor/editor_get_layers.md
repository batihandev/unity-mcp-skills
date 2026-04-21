# editor_get_layers

Get all named layers (indices 0–31) defined in the project's layer settings.

**Signature:** `EditorGetLayers()`

**Returns:** `{ layers: [{ index, name }] }` — only layers with non-empty names are included.

Note: no top-level `success` key.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var layers = Enumerable.Range(0, 32)
            .Select(i => new { index = i, name = LayerMask.LayerToName(i) })
            .Where(l => !string.IsNullOrEmpty(l.name))
            .ToArray();

        result.SetResult(new { layers });
    }
}
```
