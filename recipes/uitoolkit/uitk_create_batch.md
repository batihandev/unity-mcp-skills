# uitk_create_batch

Create multiple USS and/or UXML files in a single batched call.

**Signature:** `UitkCreateBatch(items string)`

**Returns:** `{ totalRequested, succeeded, failed, results[] }`

**Notes:**
- `items` is a JSON array of objects: `[{ "type": "uss"|"uxml", "savePath": "...", "content": "...", "ussPath": "..." }]`.
- `type` and `savePath` are required for each item; `content` and `ussPath` are optional.
- Asset editing is suspended for the duration of the batch for performance.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.SafePath`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.SnapshotObject`

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

internal sealed class _UitkFileItem
{
    public string type;
    public string savePath;
    public string content;
    public string ussPath;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _UitkFileItem { type = "uss", savePath = "Assets/UI/Theme.uss" },
            new _UitkFileItem { type = "uxml", savePath = "Assets/UI/Layout.uxml", ussPath = "Assets/UI/Theme.uss" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;
        var utf8NoBom = new UTF8Encoding(false);

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var item in items)
            {
                var target = item.savePath;
                if (string.IsNullOrEmpty(item.type)) { results.Add(new { target, success = false, error = "type is required ('uss' or 'uxml')" }); failCount++; continue; }
                if (string.IsNullOrEmpty(item.savePath)) { results.Add(new { target, success = false, error = "savePath is required" }); failCount++; continue; }
                if (Validate.SafePath(item.savePath, "savePath") is object pathErr) { results.Add(new { target, success = false, error = "Invalid savePath" }); failCount++; continue; }
                if (File.Exists(item.savePath)) { results.Add(new { target, success = false, error = "File already exists: " + item.savePath }); failCount++; continue; }

                var dir = Path.GetDirectoryName(item.savePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string lowered = item.type.ToLowerInvariant();
                string fileContent;
                if (lowered == "uss")
                {
                    fileContent = item.content ?? ("/* " + Path.GetFileNameWithoutExtension(item.savePath) + " */\n");
                }
                else if (lowered == "uxml")
                {
                    if (item.content != null)
                    {
                        fileContent = item.content;
                    }
                    else if (!string.IsNullOrEmpty(item.ussPath))
                    {
                        var uxmlDir = Path.GetDirectoryName(item.savePath)?.Replace('\\', '/') ?? "";
                        var ussDir = Path.GetDirectoryName(item.ussPath)?.Replace('\\', '/') ?? "";
                        var relUss = (uxmlDir == ussDir) ? Path.GetFileName(item.ussPath) : item.ussPath;
                        fileContent = "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\"><Style src=\"" + relUss + "\" /></ui:UXML>\n";
                    }
                    else
                    {
                        fileContent = "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\"></ui:UXML>\n";
                    }
                }
                else
                {
                    results.Add(new { target, success = false, error = "Unknown type '" + item.type + "', expected 'uss' or 'uxml'" });
                    failCount++;
                    continue;
                }

                File.WriteAllText(item.savePath, fileContent, utf8NoBom);
                AssetDatabase.ImportAsset(item.savePath);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.savePath);
                if (asset != null) WorkflowManager.SnapshotObject(asset, SnapshotType.Created);

                results.Add(new { target, success = true, path = item.savePath, lines = fileContent.Split('\n').Length });
                successCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
