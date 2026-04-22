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

## Parameters (typed-array shape)

The upstream JSON-string form is replaced by a typed `_SOFieldItem[]` — `{ name, value }` per item. Agents pass a native C# array, not JSON.

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

internal sealed class _SOFieldItem
{
    public string name;
    public string value;
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Data/MyAsset.asset";
        var fields = new[]
        {
            new _SOFieldItem { name = "health", value = "100" },
            new _SOFieldItem { name = "speed", value = "5.5" },
        };

        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (asset == null) { result.SetResult(new { error = $"ScriptableObject not found: {assetPath}" }); return; }
        if (fields == null || fields.Length == 0) { result.SetResult(new { error = "No fields provided" }); return; }

        WorkflowManager.SnapshotObject(asset);
        Undo.RecordObject(asset, "Set SO Batch");

        var type = asset.GetType();
        var allFields = type.GetFields();
        int set = 0;
        var notFound = new List<string>();
        foreach (var item in fields)
        {
            // GetFields() + FirstOrDefault keeps us off the BindingFlags reformatter NRE.
            var field = allFields.FirstOrDefault(f => !f.IsStatic && f.Name == item.name);
            if (field == null) { notFound.Add(item.name); continue; }
            field.SetValue(asset, ComponentSkills.ConvertValue(item.value, field.FieldType));
            set++;
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        result.SetResult(new { success = true, fieldsSet = set, notFound });
    }
}
```
