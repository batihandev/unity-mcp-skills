# editor_get_selection

Get all currently selected GameObjects.

**Signature:** `EditorGetSelection()`

**Returns:** `{ count, objects: [{ name, instanceId }] }`

Note: no top-level `success` key — check that `count >= 0` to confirm a valid response.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var selected = Selection.gameObjects.Select(go => new
        {
            name = go.name,
            instanceId = go.GetInstanceID()
        }).ToArray();

        result.SetResult(new { count = selected.Length, objects = selected });
    }
}
```
