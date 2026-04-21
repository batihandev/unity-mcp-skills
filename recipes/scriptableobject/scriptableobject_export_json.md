# scriptableobject_export_json

Export a ScriptableObject's data to JSON format.

**Signature:** `ScriptableObjectExportJson(string assetPath, string savePath = null)`

**Returns:** `{ success, path }` when `savePath` is provided, or `{ success, json }` when returned inline.

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `assetPath` | string | Yes | - | Asset path of the ScriptableObject to export |
| `savePath` | string | No | null | File path to save the JSON output; if omitted, JSON is returned inline |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/skills_common.md` — for `SkillsCommon.*`

## Notes

- Uses `EditorJsonUtility.ToJson` with pretty-print enabled.
- When `savePath` is provided it is validated and written as UTF-8 without BOM.

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Data/MyAsset.asset"; // Asset path of the ScriptableObject
        string savePath = null; // Optional: file path to save JSON (e.g. "Assets/Export/MyAsset.json")

        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (asset == null) { result.SetResult(new { error = $"ScriptableObject not found: {assetPath}" }); return; }

        var json = EditorJsonUtility.ToJson(asset, true);

        if (!string.IsNullOrEmpty(savePath))
        {
            if (Validate.SafePath(savePath, "savePath") is object pathErr) { result.SetResult(pathErr); return; }
            File.WriteAllText(savePath, json, SkillsCommon.Utf8NoBom);
            result.SetResult(new { success = true, path = savePath });
            return;
        }

        result.SetResult(new { success = true, json });
    }
}
```
