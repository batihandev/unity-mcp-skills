# package_list

List all installed packages currently cached by UnitySkills.

> **Native tool first:** Prefer `Unity_PackageManager_GetData` for listing packages. Use `Unity_RunCommand` with this recipe only when you need custom filtering or post-processing of the package list.

**Signature:** `PackageList()`

**Returns:** `{ success, count, packages[] }` — each package has `name`, `version`, `displayName`.

**Note:** Returns `{ error }` if the cache is not ready. Call `package_refresh` first.

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var packages = PackageManagerHelper.InstalledPackages;
        if (packages == null)
        {
            result.SetResult(new { error = "Package list not ready. Call package_refresh first." });
            return;
        }

        var list = packages.Values
            .Select(p => new { name = p.name, version = p.version, displayName = p.displayName })
            .ToList();

        result.SetResult(new { success = true, count = list.Count, packages = list });
    }
}
```
