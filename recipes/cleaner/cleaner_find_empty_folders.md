# cleaner_find_empty_folders

Find empty folders in the project. A folder is considered empty if it contains no non-meta files and all its subfolders are also empty.

**Signature:** `CleanerFindEmptyFolders(string searchPath = "Assets")`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `searchPath` | string | No | "Assets" | Root path to search within |

## Returns

```json
{
  "success": true,
  "count": 3,
  "folders": [
    "Assets/Old/EmptySubfolder",
    "Assets/Old/AnotherEmpty",
    "Assets/Old"
  ]
}
```

## Notes

- `.meta` files are ignored when determining emptiness — a folder containing only `.meta` files is still considered empty.
- The recursive algorithm reports leaf-empty folders and their parent folders when all children are also empty.
- Folders are returned in discovery order (depth-first). Use `cleaner_delete_empty_folders` to remove them — it processes longest paths first to avoid parent-before-child deletion errors.
- This skill is read-only; it does not delete anything.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

## C# Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string searchPath = "Assets";

        var empty = new List<string>();
        FindEmptyFoldersRecursive(searchPath, empty);

        result.SetResult(new { success = true, count = empty.Count, folders = empty });
    }

    private bool FindEmptyFoldersRecursive(string path, List<string> results)
    {
        var dirs = Directory.GetDirectories(path);
        var files = Directory.GetFiles(path).Where(f => !f.EndsWith(".meta")).ToArray();
        bool allSubEmpty = true;
        foreach (var dir in dirs)
            if (!FindEmptyFoldersRecursive(dir, results)) allSubEmpty = false;
        if (files.Length == 0 && (dirs.Length == 0 || allSubEmpty))
        {
            results.Add(path.Replace("\\", "/"));
            return true;
        }
        return false;
    }
}
```
