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

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // items: JSON array — each entry has type ("uss" or "uxml"), savePath, optional content and ussPath
        string items = "[{\"type\":\"uss\",\"savePath\":\"Assets/UI/Theme.uss\"},{\"type\":\"uxml\",\"savePath\":\"Assets/UI/Layout.uxml\",\"ussPath\":\"Assets/UI/Theme.uss\"}]";

        result.SetResult(BatchExecutor.Execute<UitkFileItem>(
            items,
            item =>
            {
                if (string.IsNullOrEmpty(item.type))
                    return new { error = "type is required ('uss' or 'uxml')" };
                if (string.IsNullOrEmpty(item.savePath))
                    return new { error = "savePath is required" };

                return item.type.ToLowerInvariant() == "uss"
                    ? UIToolkitSkills.UitkCreateUss(item.savePath, item.content)
                    : item.type.ToLowerInvariant() == "uxml"
                        ? UIToolkitSkills.UitkCreateUxml(item.savePath, item.content, item.ussPath)
                        : (object)new { error = $"Unknown type '{item.type}', expected 'uss' or 'uxml'" };
            },
            item => item.savePath,
            AssetDatabase.StartAssetEditing,
            AssetDatabase.StopAssetEditing
        ));
    }

    private class UitkFileItem
    {
        public string type     { get; set; }
        public string savePath { get; set; }
        public string content  { get; set; }
        public string ussPath  { get; set; }
    }
}
```
