# prefab_set_property

Set a serialized property on a component inside a prefab asset file directly, without instantiating it. Supports basic types, vectors, colors, enums, and asset references.

**Signature:** `PrefabSetProperty(string prefabPath = null, string componentType = null, string propertyName = null, string value = null, string assetReferencePath = null, string gameObjectName = null)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `prefabPath` | string | Yes | - | Path to the prefab asset |
| `componentType` | string | Yes | - | Component type name (e.g. `Rigidbody`, `Light`) |
| `propertyName` | string | Yes | - | Serialized property name |
| `value` | string | Cond. | null | Value for basic types (int/float/bool/string/enum/vector/color) |
| `assetReferencePath` | string | Cond. | null | Asset path for Object reference fields (Material, Texture, AudioClip, etc.) |
| `gameObjectName` | string | No | null | Child object name inside the prefab (defaults to root) |

Provide exactly one of `value` or `assetReferencePath`.

## Returns

```json
{
  "success": true,
  "prefabPath": "Assets/Prefabs/Enemy.prefab",
  "gameObject": "Enemy",
  "component": "Light",
  "property": "m_Intensity",
  "valueSet": "2.5"
}
```

On property not found:
```json
{
  "error": "Property 'intensity' not found on Light",
  "availableProperties": ["m_Enabled", "m_Type", "m_Color", "m_Intensity"]
}
```

## Notes

- Edits the prefab asset on disk via `SerializedObject` — no scene instance is created.
- Property name lookup tries exact match, then `m_PropertyName`, then `_propertyName`, then `m_propertyName` (lowercase).
- `prefab_edit` and `prefab_modify` do not exist — use this command to set properties directly on a prefab asset.
- To modify a live scene instance, use `component` module skills then `prefab_apply`.
- Calls `EditorUtility.SetDirty` and `AssetDatabase.SaveAssets` after applying.

### Supported value types for `value`

| Type | Example |
|------|---------|
| int / float | `"2"`, `"3.14"` |
| bool | `"true"`, `"false"`, `"1"`, `"0"` |
| string | `"Hello"` |
| enum | name (`"Loop"`) or index (`"2"`) |
| Vector2/3/4 | `"1,0,0"` |
| Color | `"1,0,0,1"` (RGBA) |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md), [`component_type_finder`](../_shared/component_type_finder.md), [`value_converter`](../_shared/value_converter.md), [`skills_common`](../_shared/skills_common.md)

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string prefabPath = null;
        string componentType = null;
        string propertyName = null;
        string value = null;
        string assetReferencePath = null;
        string gameObjectName = null;

        if (Validate.Required(prefabPath, "prefabPath") is object reqErr1) { result.SetResult(reqErr1); return; }
        if (Validate.SafePath(prefabPath, "prefabPath") is object pathErr) { result.SetResult(pathErr); return; }
        if (Validate.Required(componentType, "componentType") is object reqErr2) { result.SetResult(reqErr2); return; }
        if (Validate.Required(propertyName, "propertyName") is object reqErr3) { result.SetResult(reqErr3); return; }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { result.SetResult(new { error = $"Prefab not found: {prefabPath}" }); return; }

        // Find target GameObject inside prefab (root or child by name)
        GameObject targetGo = prefab;
        if (!string.IsNullOrEmpty(gameObjectName))
        {
            var child = prefab.transform.Find(gameObjectName);
            if (child == null)
            {
                // Deep search
                foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == gameObjectName) { child = t; break; }
                }
            }
            if (child == null)
                { result.SetResult(new { error = $"Child GameObject '{gameObjectName}' not found in prefab" }); return; }
            targetGo = child.gameObject;
        }

        // Find component
        var compType = ComponentSkills.FindComponentType(componentType);
        if (compType == null)
            { result.SetResult(new { error = $"Component type not found: {componentType}" }); return; }

        var comp = targetGo.GetComponent(compType);
        if (comp == null)
            { result.SetResult(new { error = $"Component '{componentType}' not found on '{targetGo.name}' in prefab" }); return; }

        // Use SerializedObject to edit prefab asset
        var so = new SerializedObject(comp);
        var prop = FindSerializedProperty(so, propertyName);
        if (prop == null)
            { result.SetResult(new { error = $"Property '{propertyName}' not found on {componentType}", availableProperties = ListSerializedProperties(so) }); return; }

        WorkflowManager.SnapshotObject(comp);

        // Set value based on property type
        if (!string.IsNullOrEmpty(assetReferencePath))
        {
            if (prop.propertyType != SerializedPropertyType.ObjectReference)
                { result.SetResult(new { error = $"Property '{propertyName}' is not an Object reference field (type: {prop.propertyType})" }); return; }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetReferencePath);
            if (asset == null)
                { result.SetResult(new { error = $"Asset not found: {assetReferencePath}" }); return; }

            prop.objectReferenceValue = asset;
        }
        else if (!string.IsNullOrEmpty(value))
        {
            if (!SetSerializedPropertyValue(prop, value))
                { result.SetResult(new { error = $"Failed to set value '{value}' on property '{propertyName}' (type: {prop.propertyType})" }); return; }
        }
        else
        {
            { result.SetResult(new { error = "Either 'value' or 'assetReferencePath' must be provided" }); return; }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(comp);
        AssetDatabase.SaveAssets();

        { result.SetResult(new
        {
            success = true,
            prefabPath,
            gameObject = targetGo.name,
            component = componentType,
            property = propertyName,
            valueSet = !string.IsNullOrEmpty(assetReferencePath) ? assetReferencePath : value
        }); return; }
    }

    private static SerializedProperty FindSerializedProperty(SerializedObject so, string propertyName)
    {
        var prop = so.FindProperty(propertyName);
        if (prop != null) return prop;
        var mName = "m_" + char.ToUpper(propertyName[0]) + propertyName.Substring(1);
        prop = so.FindProperty(mName);
        if (prop != null) return prop;
        prop = so.FindProperty("_" + propertyName);
        if (prop != null) return prop;
        return so.FindProperty("m_" + propertyName);
    }

    private static bool SetSerializedPropertyValue(SerializedProperty prop, string value)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                if (int.TryParse(value, out var intVal)) { prop.intValue = intVal; return true; }
                if (long.TryParse(value, out var longVal)) { prop.longValue = longVal; return true; }
                return false;
            case SerializedPropertyType.Float:
                if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var floatVal)) { prop.floatValue = floatVal; return true; }
                if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var doubleVal)) { prop.doubleValue = doubleVal; return true; }
                return false;
            case SerializedPropertyType.Boolean:
                var lower = value.ToLower().Trim();
                prop.boolValue = lower == "true" || lower == "1" || lower == "yes" || lower == "on";
                return true;
            case SerializedPropertyType.String:
                prop.stringValue = value; return true;
            case SerializedPropertyType.Enum:
                if (prop.enumDisplayNames != null)
                    for (int i = 0; i < prop.enumDisplayNames.Length; i++)
                        if (string.Equals(prop.enumDisplayNames[i], value, System.StringComparison.OrdinalIgnoreCase)) { prop.enumValueIndex = i; return true; }
                if (int.TryParse(value, out var enumIdx)) { prop.enumValueIndex = enumIdx; return true; }
                return false;
            case SerializedPropertyType.Color:
                var color = ComponentSkills.ConvertValue(value, typeof(Color));
                if (color is Color c) { prop.colorValue = c; return true; } return false;
            case SerializedPropertyType.Vector2:
                var v2 = ComponentSkills.ConvertValue(value, typeof(Vector2));
                if (v2 is Vector2 vec2) { prop.vector2Value = vec2; return true; } return false;
            case SerializedPropertyType.Vector3:
                var v3 = ComponentSkills.ConvertValue(value, typeof(Vector3));
                if (v3 is Vector3 vec3) { prop.vector3Value = vec3; return true; } return false;
            case SerializedPropertyType.Vector4:
                var v4 = ComponentSkills.ConvertValue(value, typeof(Vector4));
                if (v4 is Vector4 vec4) { prop.vector4Value = vec4; return true; } return false;
            case SerializedPropertyType.Rect:
                var rect = ComponentSkills.ConvertValue(value, typeof(Rect));
                if (rect is Rect r) { prop.rectValue = r; return true; } return false;
            case SerializedPropertyType.Bounds:
                var bounds = ComponentSkills.ConvertValue(value, typeof(Bounds));
                if (bounds is Bounds b) { prop.boundsValue = b; return true; } return false;
            case SerializedPropertyType.Vector2Int:
                var v2i = ComponentSkills.ConvertValue(value, typeof(Vector2Int));
                if (v2i is Vector2Int vec2i) { prop.vector2IntValue = vec2i; return true; } return false;
            case SerializedPropertyType.Vector3Int:
                var v3i = ComponentSkills.ConvertValue(value, typeof(Vector3Int));
                if (v3i is Vector3Int vec3i) { prop.vector3IntValue = vec3i; return true; } return false;
            case SerializedPropertyType.LayerMask:
                if (int.TryParse(value, out var mask)) { prop.intValue = mask; return true; }
                var layer = LayerMask.NameToLayer(value);
                if (layer >= 0) { prop.intValue = 1 << layer; return true; }
                return false;
            default: return false;
        }
    }

    private static string[] ListSerializedProperties(SerializedObject so)
    {
        var names = new System.Collections.Generic.List<string>();
        var prop = so.GetIterator();
        bool enter = true;
        while (prop.NextVisible(enter) && names.Count < 30)
        {
            enter = false;
            if (prop.name == "m_Script") continue;
            names.Add(prop.name);
        }
        return names.ToArray();
    }
}
```
