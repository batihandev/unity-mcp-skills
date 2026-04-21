# package_check

Check whether a specific package is installed and retrieve its version.

> **Native tool first:** Prefer `Unity_PackageManager_GetData` for checking package status. Use `Unity_RunCommand` with this recipe only when you need to check multiple packages or combine the result with other logic.

**Signature:** `PackageCheck(string packageId)`

**Parameters:**
- `packageId` — Required. Package ID such as `com.unity.cinemachine`.

**Returns:** `{ packageId, installed, version }` — `version` is `null` when not installed.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string packageId = "com.unity.cinemachine"; // Replace with target package ID

        if (Validate.Required(packageId, "packageId") is object err) { result.SetResult(err); return; }

        var installed = PackageManagerHelper.IsPackageInstalled(packageId);
        var version = PackageManagerHelper.GetInstalledVersion(packageId);

        result.SetResult(new { packageId, installed, version });
    }
}
```
