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

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        /* Original Logic:

            if (Validate.Required(prefabPath, "prefabPath") is object reqErr1) return reqErr1;
            if (Validate.SafePath(prefabPath, "prefabPath") is object pathErr) return pathErr;
            if (Validate.Required(componentType, "componentType") is object reqErr2) return reqErr2;
            if (Validate.Required(propertyName, "propertyName") is object reqErr3) return reqErr3;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return new { error = $"Prefab not found: {prefabPath}" };

            GameObject targetGo = prefab;
            if (!string.IsNullOrEmpty(gameObjectName))
            {
                var child = prefab.transform.Find(gameObjectName);
                if (child == null)
                {
                    foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == gameObjectName) { child = t; break; }
                    }
                }
                if (child == null)
                    return new { error = $"Child GameObject '{gameObjectName}' not found in prefab" };
                targetGo = child.gameObject;
            }

            var compType = ComponentSkills.FindComponentType(componentType);
            if (compType == null)
                return new { error = $"Component type not found: {componentType}" };

            var comp = targetGo.GetComponent(compType);
            if (comp == null)
                return new { error = $"Component '{componentType}' not found on '{targetGo.name}' in prefab" };

            var so = new SerializedObject(comp);
            var prop = FindSerializedProperty(so, propertyName);
            if (prop == null)
                return new { error = $"Property '{propertyName}' not found on {componentType}", availableProperties = ListSerializedProperties(so) };

            WorkflowManager.SnapshotObject(comp);

            if (!string.IsNullOrEmpty(assetReferencePath))
            {
                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                    return new { error = $"Property '{propertyName}' is not an Object reference field (type: {prop.propertyType})" };
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetReferencePath);
                if (asset == null)
                    return new { error = $"Asset not found: {assetReferencePath}" };
                prop.objectReferenceValue = asset;
            }
            else if (!string.IsNullOrEmpty(value))
            {
                if (!SetSerializedPropertyValue(prop, value))
                    return new { error = $"Failed to set value '{value}' on property '{propertyName}' (type: {prop.propertyType})" };
            }
            else
            {
                return new { error = "Either 'value' or 'assetReferencePath' must be provided" };
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(comp);
            AssetDatabase.SaveAssets();

            return new
            {
                success = true,
                prefabPath,
                gameObject = targetGo.name,
                component = componentType,
                property = propertyName,
                valueSet = !string.IsNullOrEmpty(assetReferencePath) ? assetReferencePath : value
            };
        */
    }
}
```
