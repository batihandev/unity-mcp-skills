# cleaner_delete_empty_folders

Delete all empty folders under a search path. Processes deepest paths first to avoid parent-before-child ordering issues.

**Signature:** `CleanerDeleteEmptyFolders(string searchPath = "Assets")`

## Returns

```json
{
  "success": true,
  "deleted": 3,
  "total": 3
}
```

`total` = total empty folders found; `deleted` = folders successfully deleted by `AssetDatabase.MoveAssetToTrash`.

## Notes

- A folder is considered empty if it has no non-meta files and all its subfolders are also empty.
- Folders are deleted in **descending path-length order** (deepest first) to ensure child folders are removed before their parents.
- `AssetDatabase.Refresh()` is called after all deletions.
- To preview which folders will be deleted without removing them, use `cleaner_find_empty_folders` first.
- This operation is tracked by the workflow manager (`TracksWorkflow = true`).

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

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

        int deleted = 0;
        foreach (var folder in empty.OrderByDescending(f => f.Length))
        {
            if (AssetDatabase.MoveAssetToTrash(folder)) deleted++;
        }
        AssetDatabase.Refresh();

        result.SetResult(new { success = true, deleted, total = empty.Count });
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
