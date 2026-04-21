# package_get_versions

Get available versions for one installed package.

> **Native tool first:** Prefer `Unity_PackageManager_GetData` for version inspection. Use `Unity_RunCommand` with this recipe only when you need to process version data programmatically.

**Signature:** `PackageGetVersions(string packageId)`

**Parameters:**
- `packageId` — Required. Installed package ID.

**Returns:** `{ success, packageId, currentVersion, compatibleVersion, latestVersion, allVersions[] }`.

**Note:** Returns `{ error }` if the cache is not ready or the package is not found.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string packageId = "com.unity.cinemachine"; // Replace with installed package ID

        if (Validate.Required(packageId, "packageId") is object err) { result.SetResult(err); return; }

        var packages = PackageManagerHelper.InstalledPackages;
        if (packages == null)
        {
            result.SetResult(new { error = "Package list not ready. Call package_refresh first." });
            return;
        }

        if (!packages.TryGetValue(packageId, out var pkg))
        {
            result.SetResult(new { error = $"Package not found: {packageId}" });
            return;
        }

        var versions = pkg.versions?.all?.ToList();
        result.SetResult(new
        {
            success = true,
            packageId,
            currentVersion = pkg.version,
            compatibleVersion = pkg.versions?.compatible,
            latestVersion = pkg.versions?.latest,
            allVersions = versions
        });
    }
}
```
