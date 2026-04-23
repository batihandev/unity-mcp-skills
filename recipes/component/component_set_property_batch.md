# component_set_property_batch

Set properties on components across multiple GameObjects in a single call. Supports the same value types as `component_set_property`.

**Signature:** `ComponentSetPropertyBatch(string items)`

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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md), [`component_type_finder`](../_shared/component_type_finder.md), [`value_converter`](../_shared/value_converter.md), [`skills_common`](../_shared/skills_common.md)

## Notes

The `value` and `assetPath` input paths are supported. Cross-scene reference resolution (`referencePath` / `referenceName` from upstream) is out of scope — use a dedicated recipe if you need that.

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

internal sealed class _BatchSetPropertyItem
{
    public string name;
    public int instanceId;
    public string path;
    public string componentType;
    public string propertyName;
    public string value;      // primitive value, CSV vector, hex color, etc. — see ComponentSkills.ConvertValue
    public string assetPath;  // asset reference alternative
}

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var items = new[]
        {
            new _BatchSetPropertyItem { name = "Player", componentType = "Rigidbody", propertyName = "mass", value = "5" },
            new _BatchSetPropertyItem { instanceId = 12346, componentType = "MeshRenderer", propertyName = "sharedMaterial", assetPath = "Assets/Materials/Metal.mat" },
            new _BatchSetPropertyItem { path = "Level/Light", componentType = "Light", propertyName = "color", value = "#FF8800" },
        };

        var results = new List<object>();
        int successCount = 0, failCount = 0;

        foreach (var item in items)
        {
            var target = item.name ?? item.path ?? ("#" + item.instanceId);
            if (string.IsNullOrEmpty(item.componentType) || string.IsNullOrEmpty(item.propertyName))
            { results.Add(new { target, success = false, error = "componentType and propertyName required" }); failCount++; continue; }

            var (go, err) = GameObjectFinder.FindOrError(item.name, item.instanceId, item.path);
            if (err != null) { results.Add(new { target, success = false, error = "Object not found" }); failCount++; continue; }

            var type = ComponentSkills.FindComponentType(item.componentType);
            if (type == null) { results.Add(new { target, success = false, error = "Component type not found: " + item.componentType }); failCount++; continue; }

            var comp = go.GetComponent(type);
            if (comp == null) { results.Add(new { target, success = false, error = "Component not found: " + item.componentType }); failCount++; continue; }

            var prop = type.GetProperties().FirstOrDefault(p => string.Equals(p.Name, item.propertyName, System.StringComparison.OrdinalIgnoreCase));
            var field = prop == null ? type.GetFields().FirstOrDefault(f => string.Equals(f.Name, item.propertyName, System.StringComparison.OrdinalIgnoreCase)) : null;
            if (prop == null && field == null)
            { results.Add(new { target, success = false, error = "Property/field not found: " + item.propertyName }); failCount++; continue; }

            WorkflowManager.SnapshotObject(comp);
            Undo.RecordObject(comp, "Batch Set Property");

            var targetType = prop?.PropertyType ?? field.FieldType;
            object converted;
            if (!string.IsNullOrEmpty(item.assetPath))
            {
                converted = AssetDatabase.LoadAssetAtPath(item.assetPath, targetType);
                if (converted == null) { results.Add(new { target, success = false, error = "Asset not found or type mismatch: " + item.assetPath }); failCount++; continue; }
            }
            else
            {
                converted = ComponentSkills.ConvertValue(item.value, targetType);
            }

            if (prop != null && prop.CanWrite) prop.SetValue(comp, converted);
            else if (field != null) field.SetValue(comp, converted);
            else { results.Add(new { target, success = false, error = "Property is read-only: " + item.propertyName }); failCount++; continue; }

            EditorUtility.SetDirty(comp);
            results.Add(new { target = go.name, success = true, property = item.propertyName });
            successCount++;
        }

        result.SetResult(new { success = failCount == 0, totalItems = items.Length, successCount, failCount, results });
    }
}
```
