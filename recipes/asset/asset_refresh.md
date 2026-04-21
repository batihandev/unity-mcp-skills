# asset_refresh

Refresh the AssetDatabase after external file changes. No parameters required.

**Signature:** `AssetRefresh()`

**Returns:** `{ success, message }` — plus a `serverAvailability` notice (conditional: only emitted when Unity may recompile scripts).

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // No parameters — just call and set result
        AssetDatabase.Refresh();

        var res = new Dictionary<string, object>
        {
            ["success"] = true,
            ["message"] = "Asset database refreshed"
        };

        ServerAvailabilityHelper.AttachTransientUnavailableNotice(
            res,
            "AssetDatabase.Refresh may trigger a short asset refresh window. The REST server can be briefly unavailable if Unity starts recompiling scripts.",
            alwaysInclude: false);

        result.SetResult(res);
    }
}
```
