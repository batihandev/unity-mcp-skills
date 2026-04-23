# asset_refresh

Refresh the AssetDatabase after external file changes. No parameters required.

**Signature:** `AssetRefresh()`

**Returns:** `{ success, message }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

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
