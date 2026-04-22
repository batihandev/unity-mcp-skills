# asset_refresh

Refresh the AssetDatabase after external file changes. No parameters required.

**Signature:** `AssetRefresh()`

**Returns:** `{ success, message }`

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
        AssetDatabase.Refresh();
        result.SetResult(new { success = true, message = "Asset database refreshed" });
    }
}
```
