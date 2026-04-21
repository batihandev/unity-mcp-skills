# component_set_property_batch

Set properties on components across multiple GameObjects in a single call. Supports the same value types as `component_set_property`.

**Signature:** `ComponentSetPropertyBatch(string items)`

## Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `items` | string | Yes | JSON array of batch items |

### Batch Item Schema

```json
[
  {
    "name": "Player",
    "componentType": "Rigidbody",
    "propertyName": "mass",
    "value": "5"
  },
  {
    "instanceId": 12346,
    "componentType": "MeshRenderer",
    "propertyName": "sharedMaterial",
    "assetPath": "Assets/Materials/Metal.mat"
  },
  {
    "path": "Level/Light",
    "componentType": "Light",
    "propertyName": "color",
    "value": "#FF8800"
  }
]
```

Each item supports: `name`, `instanceId`, `path` (at least one required), `componentType` (required), `propertyName` (required), and one of `value`, `referencePath`/`referenceName`, or `assetPath`.

## Returns

```json
{
  "success": true,
  "totalItems": 3,
  "successCount": 3,
  "failCount": 0,
  "results": [
    { "target": "Player", "success": true, "property": "mass" },
    { "target": "Prop", "success": true, "property": "sharedMaterial" },
    { "target": "Light", "success": true, "property": "color" }
  ]
}
```

## Notes

- Reduces N API calls to 1 — always prefer batch for 2+ property sets.
- Property name lookup is case-insensitive as a fallback.
- Each item is processed independently; failures in one item do not block others.
- Snapshots each component for workflow undo before modifying.

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        /* Original Logic:

            return BatchExecutor.Execute<BatchSetPropertyItem>(items, item =>
            {
                if (string.IsNullOrEmpty(item.componentType) || string.IsNullOrEmpty(item.propertyName))
                    throw new System.Exception("componentType and propertyName required");

                var (go, error) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
                if (error != null) throw new System.Exception("Object not found");

                var type = FindComponentType(item.componentType);
                if (type == null)
                    throw new System.Exception($"Component type not found: {item.componentType}");

                var comp = go.GetComponent(type);
                if (comp == null)
                    throw new System.Exception($"Component not found: {item.componentType}");

                var (prop, field) = FindMember(type, item.propertyName);

                if (prop == null && field == null)
                    throw new System.Exception($"Property/field not found: {item.propertyName}");

                WorkflowManager.SnapshotObject(comp);
                Undo.RecordObject(comp, "Batch Set Property");

                var targetType = prop?.PropertyType ?? field.FieldType;
                object converted;

                if (!string.IsNullOrEmpty(item.assetPath))
                {
                    converted = ResolveAssetReference(targetType, item.assetPath);
                    if (converted == null)
                        throw new System.Exception($"Asset not found or type mismatch: '{item.assetPath}' (expected {targetType.Name})");
                }
                else if (!string.IsNullOrEmpty(item.referencePath) || !string.IsNullOrEmpty(item.referenceName))
                {
                    converted = ResolveReference(targetType, item.referencePath, item.referenceName);
                    if (converted == null)
                        throw new System.Exception($"Reference resolution failed for {item.propertyName}");
                }
                else
                {
                    var valStr = item.value?.ToString();
                    converted = ConvertValue(valStr, targetType);
                }

                if (prop != null && prop.CanWrite)
                    prop.SetValue(comp, converted);
                else if (field != null)
                    field.SetValue(comp, converted);
                else
                    throw new System.Exception($"Property {item.propertyName} is read-only");

                EditorUtility.SetDirty(comp);
                return new { target = go.name, success = true, property = item.propertyName };
            }, item => item.name ?? item.path);
        */
    }
}
```
