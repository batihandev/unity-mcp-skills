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

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`component_type_finder`](../_shared/component_type_finder.md), [`skills_common`](../_shared/skills_common.md)

## C# Template

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null; int instanceId = 0; string path = null;
        string componentType = "Rigidbody";
        bool includePrivate = false;

        if (Validate.Required(componentType, "componentType") is object err) { result.SetResult(err); return; }

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var type = ComponentSkills.FindComponentType(componentType);
        if (type == null)
            { result.SetResult(new { error = $"Component type not found: {componentType}" }); return; }

        var comp = go.GetComponent(type);
        if (comp == null)
            { result.SetResult(new { error = $"Component not found: {componentType}" }); return; }

        var props = type.GetProperties()
            .Where(p => p.CanRead && !p.GetIndexParameters().Any() && (p.GetGetMethod() != null))
            .Select(p =>
            {
                try
                {
                    var propName = p.Name;
                    // Redirect material/materials to shared variants to avoid instantiation in editor
                    if (comp is Renderer && (propName == "material" || propName == "materials"))
                        propName = propName == "material" ? "sharedMaterial" : "sharedMaterials";
                    var readProp = type.GetProperty(propName) ?? p;
                    var val = readProp.GetValue(comp, null);
                    return new { name = p.Name, type = p.PropertyType.Name, fullType = p.PropertyType.FullName, value = FormatValue(val), canWrite = p.CanWrite };
                }
                catch { return new { name = p.Name, type = p.PropertyType.Name, fullType = p.PropertyType.FullName, value = "(error reading)", canWrite = p.CanWrite }; }
            })
            .ToArray();

        var fields = type.GetFields()
            .Where(f => !f.Name.StartsWith("<"))
            .Select(f =>
            {
                try
                {
                    var val = f.GetValue(comp);
                    return new { name = f.Name, type = f.FieldType.Name, fullType = f.FieldType.FullName, value = FormatValue(val), isSerializable = f.IsPublic || f.GetCustomAttributes(typeof(SerializeField), false).Length > 0 };
                }
                catch { return new { name = f.Name, type = f.FieldType.Name, fullType = f.FieldType.FullName, value = "(error reading)", isSerializable = false }; }
            })
            .ToArray();

        result.SetResult(new {
            gameObject = go.name,
            component = componentType,
            fullTypeName = type.FullName,
            properties = props,
            fields = fields
        });
    }

    private static string FormatValue(object val)
    {
        if (val == null) return "null";
        if (val is UnityEngine.Object uo) return uo != null ? uo.name : "null";
        return val.ToString();
    }
}
```
