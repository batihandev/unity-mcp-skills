# component_set_property

Set a property or field on a component. Supports scalar values, Unity math types (Vector2/3/4, Color, Quaternion), scene object references, and project asset references.

**Signature:** `ComponentSetProperty(string name = null, int instanceId = 0, string path = null, string componentType = null, string propertyName = null, string value = null, string referencePath = null, string referenceName = null, string assetPath = null)`

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | No* | GameObject name |
| `instanceId` | int | No* | Instance ID |
| `path` | string | No* | Hierarchy path |
| `componentType` | string | Yes | Component type |
| `propertyName` | string | Yes | Property or field name |
| `value` | string | Cond. | Value for basic types, vectors, colors |
| `referencePath` | string | No | Hierarchy path of a scene object reference |
| `referenceName` | string | No | Name of a scene object reference |
| `assetPath` | string | No | Project asset path (Material, Texture, AudioClip, Prefab, ScriptableObject, etc.) |

*At least one object identifier required. Provide exactly one of `value`, `referencePath`/`referenceName`, or `assetPath`.

## Returns

```json
{
  "success": true,
  "gameObject": "Player",
  "component": "Rigidbody",
  "property": "mass",
  "valueSet": "2",
  "valueType": "Single"
}
```

## Value Format Examples

| Type | Example `value` |
|------|----------------|
| float | `"2.5"` |
| int | `"10"` |
| bool | `"true"` / `"false"` / `"1"` / `"0"` |
| Vector2 | `"1.0, 2.0"` |
| Vector3 | `"1.0, 2.0, 3.0"` |
| Vector4 | `"1, 0, 0, 1"` |
| Color (float) | `"1.0, 0.5, 0.0, 1.0"` |
| Color (hex) | `"#FF8800"` |
| Color (named) | `"red"` / `"blue"` / `"white"` etc. |
| Quaternion (euler) | `"0, 90, 0"` |
| Quaternion (xyzw) | `"0, 0.707, 0, 0.707"` |
| Enum | `"ForceMode.Impulse"` or just `"Impulse"` |
| LayerMask | layer name string or int bitmask |
| AnimationCurve | `"linear"` / `"easein"` / `"easeout"` / `"easeinout"` / `"constant"` |

## Notes

- Property name lookup is case-insensitive as a fallback.
- Uses reflection — both C# properties (`CanWrite`) and public fields are supported.
- Read-only properties return an error with a list of available writable properties.
- Uses `Undo.RecordObject` — operation is undoable.
- Snapshots the component state for workflow undo before modifying.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/workflow_manager.md` — for `WorkflowManager.*`

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if (string.IsNullOrEmpty(componentType) || string.IsNullOrEmpty(propertyName))
            { result.SetResult(new { error = "componentType and propertyName are required" }); return; }

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var type = FindComponentType(componentType);
        if (type == null)
            { result.SetResult(new { error = $"Component type not found: {componentType}" }); return; }
    
        var comp = go.GetComponent(type);
        if (comp == null)
            { result.SetResult(new { error = $"Component not found: {componentType}" }); return; }

        // Find property or field (with caching)
        var (prop, field) = FindMember(type, propertyName);

        if (prop == null && field == null)
            { result.SetResult(new {
                error = $"Property/field not found: {propertyName}",
                availableProperties = GetAvailableProperties(type)
            }); return; }

        WorkflowManager.SnapshotObject(comp);
        Undo.RecordObject(comp, "Set Property");

        try
        {
            var targetType = prop?.PropertyType ?? field.FieldType;
            object converted;

            // Handle asset references (Project assets: ScriptableObject, Prefab, Material, Texture, etc.)
            if (!string.IsNullOrEmpty(assetPath))
            {
                converted = ResolveAssetReference(targetType, assetPath);
                if (converted == null)
                    { result.SetResult(new { error = $"Asset not found or type mismatch: '{assetPath}' (expected {targetType.Name})" }); return; }
            }
            // Handle scene references (Transform, GameObject, Component references)
            else if (!string.IsNullOrEmpty(referencePath) || !string.IsNullOrEmpty(referenceName))
            {
                converted = ResolveReference(targetType, referencePath, referenceName);
                if (converted == null)
                    { result.SetResult(new { error = $"Could not resolve reference for {propertyName}. Target: path='{referencePath}', name='{referenceName}'" }); return; }
            }
            else
            {
                converted = ConvertValue(value, targetType);
            }

            if (prop != null && prop.CanWrite)
                prop.SetValue(comp, converted);
            else if (field != null)
                field.SetValue(comp, converted);
            else
                { result.SetResult(new { error = $"Property {propertyName} is read-only" }); return; }

            EditorUtility.SetDirty(comp);
    
            { result.SetResult(new { 
                success = true, 
                gameObject = go.name, 
                component = componentType,
                property = propertyName, 
                valueSet = converted?.ToString() ?? "null",
                valueType = targetType.Name
            }); return; }
        }
        catch (System.Exception ex)
        {
            { result.SetResult(new { 
                error = ex.Message,
            }); return; }
        }
    }
}
```
