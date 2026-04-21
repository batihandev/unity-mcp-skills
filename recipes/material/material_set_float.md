# material_set_float

Set a float property on a material.

**Signature:** `MaterialSetFloat(string name = null, int instanceId = 0, string path = null, string propertyName = null, float value = 0)`

**Returns:** `{ success, target, property, value }`

## Notes

- Use this to set `_Metallic`, `_Glossiness` (Standard), `_Smoothness` (URP), `_Cutoff`, or any other float property.
- `propertyName` is required. Returns an error if the property does not exist on the shader.
- Use `material_get_properties` to discover available float properties and their valid ranges.
- There is no `material_set_metallic` or `material_set_smoothness` skill — use this with the appropriate `propertyName`.

## Common Property Names

| Pipeline | Metallic | Smoothness |
|----------|----------|------------|
| Standard | `_Metallic` | `_Glossiness` |
| URP | `_Metallic` | `_Smoothness` |
| HDRP | `_Metallic` | `_Smoothness` |

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name         = "Cube";       // target GameObject name
        int    instanceId   = 0;
        string path         = null;         // or material asset path
        string propertyName = "_Metallic";  // required
        float  value        = 0.8f;

        /* Original Logic:

            if (Validate.Required(propertyName, "propertyName") is object err) return err;

            var (material, go, error) = FindMaterial(name, instanceId, path);
            if (error != null) return error;

            if (!material.HasProperty(propertyName))
                return new {
                    error = $"Property not found: {propertyName}",
                    shaderName = material.shader.name,
                    suggestion = "Use material_get_properties to see available properties"
                };

            WorkflowManager.SnapshotObject(material);
            Undo.RecordObject(material, "Set Material Float");
            material.SetFloat(propertyName, value);

            if (go == null) EditorUtility.SetDirty(material);

            return new { success = true, target = go != null ? go.name : path, property = propertyName, value };
        */
    }
}
```
