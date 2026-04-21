# package_get_cinemachine_status

Get current Cinemachine and Splines installation status.

> **Native tool first:** Prefer `Unity_PackageManager_GetData` with `package_get_cinemachine_status` action. Use `Unity_RunCommand` with this recipe only when combining status with additional logic.

**Signature:** `PackageGetCinemachineStatus()`

**Parameters:** None.

**Returns:**
```json
{
  "cinemachine": { "installed": bool, "version": string, "isVersion3": bool },
  "splines":     { "installed": bool, "version": string }
}
```

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
        var status = PackageManagerHelper.GetCinemachineStatus();
        var splinesInstalled = PackageManagerHelper.IsPackageInstalled(PackageManagerHelper.SplinesPackageId);
        var splinesVersion = PackageManagerHelper.GetInstalledVersion(PackageManagerHelper.SplinesPackageId);

        result.SetResult(new
        {
            cinemachine = new
            {
                installed = status.installed,
                version = status.version,
                isVersion3 = status.isVersion3
            },
            splines = new
            {
                installed = splinesInstalled,
                version = splinesVersion
            }
        });
    }
}
```
