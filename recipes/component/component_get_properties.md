# component_get_properties

Get all readable properties and fields of a component. Returns type, value, and writability metadata for each member.

**Signature:** `ComponentGetProperties(string name = null, int instanceId = 0, string path = null, string componentType = null, bool includePrivate = false)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `name` | string | No* | null | GameObject name |
| `instanceId` | int | No* | 0 | Instance ID |
| `path` | string | No* | null | Hierarchy path |
| `componentType` | string | Yes | - | Component type |
| `includePrivate` | bool | No | false | Include non-public members |

*At least one object identifier required.

## Returns

```json
{
  "gameObject": "Player",
  "component": "Rigidbody",
  "fullTypeName": "UnityEngine.Rigidbody",
  "properties": [
    { "name": "mass", "type": "Single", "fullType": "System.Single", "value": "1", "canWrite": true },
    { "name": "drag", "type": "Single", "fullType": "System.Single", "value": "0", "canWrite": true },
    { "name": "isKinematic", "type": "Boolean", "fullType": "System.Boolean", "value": "False", "canWrite": true }
  ],
  "fields": [
    { "name": "someField", "type": "String", "fullType": "System.String", "value": "hello", "isSerializable": true }
  ]
}
```

Properties that cannot be read safely return `"(error reading)"` as their value.

## Notes

- Use this command to discover exact property names before calling `component_set_property`.
- `canWrite: false` properties will fail if passed to `component_set_property`.
- `isSerializable` is true for public fields and fields with `[SerializeField]`.
- For `MeshRenderer` / `Renderer`, reading `material` or `materials` is redirected to `sharedMaterial` / `sharedMaterials` to avoid instantiating materials in editor.
- `includePrivate: true` adds `BindingFlags.NonPublic` — output can be very large.

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        /* Original Logic:

            if (Validate.Required(componentType, "componentType") is object err) return err;

            var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
            if (error != null) return error;

            var type = FindComponentType(componentType);
            if (type == null)
                return new { error = $"Component type not found: {componentType}" };

            var comp = go.GetComponent(type);
            if (comp == null)
                return new { error = $"Component not found: {componentType}" };

            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            if (includePrivate)
                bindingFlags |= BindingFlags.NonPublic;

            var props = type.GetProperties(bindingFlags)
                .Where(p => p.CanRead && !p.GetIndexParameters().Any())
                .Select(p =>
                {
                    try
                    {
                        var val = ReadPropertyValueSafely(comp, p);
                        return new {
                            name = p.Name,
                            type = p.PropertyType.Name,
                            fullType = p.PropertyType.FullName,
                            value = FormatValue(val),
                            canWrite = p.CanWrite
                        };
                    }
                    catch { return new { name = p.Name, type = p.PropertyType.Name, fullType = p.PropertyType.FullName, value = "(error reading)", canWrite = p.CanWrite }; }
                })
                .ToArray();

            var fields = type.GetFields(bindingFlags)
                .Select(f =>
                {
                    try
                    {
                        var val = f.GetValue(comp);
                        return new {
                            name = f.Name,
                            type = f.FieldType.Name,
                            fullType = f.FieldType.FullName,
                            value = FormatValue(val),
                            isSerializable = f.IsPublic || f.GetCustomAttribute<SerializeField>() != null
                        };
                    }
                    catch { return new { name = f.Name, type = f.FieldType.Name, fullType = f.FieldType.FullName, value = "(error reading)", isSerializable = false }; }
                })
                .ToArray();

            return new {
                gameObject = go.name,
                component = componentType,
                fullTypeName = type.FullName,
                properties = props,
                fields = fields
            };
        */
    }
}
```
