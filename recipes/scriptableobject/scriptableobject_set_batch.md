# scriptableobject_set_batch

Set multiple fields on a ScriptableObject in a single operation.

**Signature:** `ScriptableObjectSetBatch(string assetPath, string fields)`

**Returns:** `{ success, fieldsSet }`

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `assetPath` | string | Yes | Asset path of the ScriptableObject |
| `fields` | string | Yes | JSON object with field-value pairs, e.g. `{"fieldName": "value", ...}` |

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`
- `recipes/_shared/value_converter.md` — for `ComponentSkills.ConvertValue`

## Notes

- Only public instance fields are set; properties are not supported in batch mode.
- Uses `ComponentSkills.ConvertValue` to coerce each string value to the field's type.
- All changes are recorded as a single Undo operation and auto-saved.

```csharp
using UnityEngine;
using UnityEditor;
using System.Reflection;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Data/MyAsset.asset"; // Asset path of the ScriptableObject
        string fields = "{\"health\": \"100\", \"speed\": \"5.5\"}"; // JSON field-value pairs

        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (asset == null) { result.SetResult(new { error = $"ScriptableObject not found: {assetPath}" }); return; }

        var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(fields);
        if (dict == null || dict.Count == 0) { result.SetResult(new { error = "No fields provided" }); return; }

        WorkflowManager.SnapshotObject(asset);
        Undo.RecordObject(asset, "Set SO Batch");

        var type = asset.GetType();
        int set = 0;
        foreach (var kv in dict)
        {
            var field = type.GetField(kv.Key, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) { field.SetValue(asset, ComponentSkills.ConvertValue(kv.Value, field.FieldType)); set++; }
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        result.SetResult(new { success = true, fieldsSet = set });
    }
}
```
