# package_search

Search the installed package cache by package name or display name.

> **Native tool first:** Prefer `Unity_PackageManager_GetData` for searching packages. Use `Unity_RunCommand` with this recipe only when you need custom matching logic beyond name/displayName.

**Signature:** `PackageSearch(string query)`

**Parameters:**
- `query` — Required. Search keyword matched case-insensitively against `name` and `displayName`.

**Returns:** `{ success, query, count, packages[] }` — each package has `name`, `version`, `displayName`.

**Warning:** Searches the **installed package cache only**. It does NOT query the Unity Registry. Call `package_refresh` first if the cache may be stale.

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string query = "cinemachine"; // Replace with search keyword

        if (Validate.Required(query, "query") is object err) { result.SetResult(err); return; }

        var packages = PackageManagerHelper.InstalledPackages;
        if (packages == null)
        {
            result.SetResult(new { error = "Package list not ready. Call package_refresh first." });
            return;
        }

        var matches = packages.Values
            .Where(p => p.name.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (p.displayName != null && p.displayName.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0))
            .Select(p => new { name = p.name, version = p.version, displayName = p.displayName })
            .ToList();

        result.SetResult(new { success = true, query, count = matches.Count, packages = matches });
    }
}
```
