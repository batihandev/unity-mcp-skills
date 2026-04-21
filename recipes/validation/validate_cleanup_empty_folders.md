# validate_cleanup_empty_folders

Find (and optionally delete) empty folders under a given root path in the project.

**Signature:** `ValidateCleanupEmptyFolders(rootPath string = "Assets", dryRun bool = true)`

**Returns:** `{ success, dryRun, emptyFolderCount, folders: [path], message }`

**Notes:**
- Folders containing only `.meta` files are treated as empty
- When `dryRun = false`, folders are deleted deepest-first to handle nested empty hierarchies
- Always preview with `dryRun = true` before committing deletions

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string rootPath = "Assets";
        bool dryRun = true;

        if (Validate.SafePath(rootPath, "rootPath") is object pathErr)
        {
            result.SetResult(pathErr);
            return;
        }

        var emptyFolders = new List<string>();
        FindEmptyFolders(rootPath, emptyFolders);

        if (!dryRun && emptyFolders.Count > 0)
        {
            var sorted = emptyFolders.OrderByDescending(f => f.Length).ToList();
            foreach (var folder in sorted)
            {
                if (Directory.Exists(folder))
                {
                    AssetDatabase.DeleteAsset(folder);
                }
            }
            AssetDatabase.Refresh();
        }

        result.SetResult(new
        {
            success = true,
            dryRun,
            emptyFolderCount = emptyFolders.Count,
            folders = emptyFolders,
            message = dryRun ? "Dry run - no folders deleted" : $"Deleted {emptyFolders.Count} empty folders"
        });
    }

    private void FindEmptyFolders(string path, List<string> emptyFolders)
    {
        if (!Directory.Exists(path)) return;

        var subDirectories = Directory.GetDirectories(path);
        foreach (var subDir in subDirectories)
        {
            FindEmptyFolders(subDir, emptyFolders);
        }

        var files = Directory.GetFiles(path);
        var directories = Directory.GetDirectories(path);

        var hasRealFiles = files.Any(f => !f.EndsWith(".meta"));
        var hasSubDirs = directories.Length > 0;

        if (!hasRealFiles && !hasSubDirs)
        {
            emptyFolders.Add(path.Replace("\\", "/"));
        }
    }
}
```
