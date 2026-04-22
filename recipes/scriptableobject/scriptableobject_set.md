# scriptableobject_set

Set a field or property on a ScriptableObject asset.

**Signature:** `ScriptableObjectSet(string assetPath, string fieldName, string value)`

**Returns:** `{ success, field, value }`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`workflow_manager`](../_shared/workflow_manager.md), [`value_converter`](../_shared/value_converter.md)

## Notes

- Supports both public fields and writable properties.
- Uses `ComponentSkills.ConvertValue` to coerce the string value to the target type.
- Change is recorded via `Undo` and auto-saved.

```csharp
using UnityEngine;
using UnityEditor;
using System.Reflection;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string assetPath = "Assets/Data/MyAsset.asset"; // Asset path of the ScriptableObject
        string fieldName = "myField"; // Field or property name
        string value = "42"; // Value to set (will be converted to the field's type)

        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (asset == null)
        {
            result.SetResult(new { error = $"ScriptableObject not found: {assetPath}" });
            return;
        }

        var type = asset.GetType();
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        var prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);

        if (field == null && prop == null)
        {
            result.SetResult(new { error = $"Field/property not found: {fieldName}" });
            return;
        }

        WorkflowManager.SnapshotObject(asset);
        Undo.RecordObject(asset, "Set ScriptableObject Field");

        try
        {
            if (field != null)
            {
                var converted = ComponentSkills.ConvertValue(value, field.FieldType);
                field.SetValue(asset, converted);
            }
            else if (prop != null && prop.CanWrite)
            {
                var converted = ComponentSkills.ConvertValue(value, prop.PropertyType);
                prop.SetValue(asset, converted);
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            result.SetResult(new { success = true, field = fieldName, value });
        }
        catch (System.Exception ex)
        {
            result.SetResult(new { error = ex.Message });
        }
    }
}
```
