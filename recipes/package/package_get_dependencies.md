# package_get_dependencies

Get dependency information for one installed package.

> **Native tool first:** Prefer `Unity_PackageManager_GetData` for dependency inspection. Use `Unity_RunCommand` with this recipe only when you need to process dependency data programmatically.

**Signature:** `PackageGetDependencies(string packageId)`

**Parameters:**
- `packageId` — Required. Installed package ID.

**Returns:** `{ success, packageId, version, dependencyCount, dependencies[] }` — each dependency has `name`, `version`.

**Note:** Returns `{ error }` if the cache is not ready or the package is not found.

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

        var deps = pkg.dependencies?.Select(d => new { name = d.name, version = d.version }).ToList();
        result.SetResult(new
        {
            success = true,
            packageId,
            version = pkg.version,
            dependencyCount = deps?.Count ?? 0,
            dependencies = deps
        });
    }
}
```
