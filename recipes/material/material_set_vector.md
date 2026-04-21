# material_set_vector

Set a Vector4 property on a material.

**Signature:** `MaterialSetVector(string name = null, int instanceId = 0, string path = null, string propertyName = null, float x = 0, float y = 0, float z = 0, float w = 0)`

**Returns:** `{ success, target, property, value: {x,y,z,w} }`

## Notes

- `propertyName` is required. Returns an error (with `shaderName`) if the property does not exist on the shader.
- Use `material_get_properties` to discover vector properties available on a material.
- All four components `x`, `y`, `z`, `w` default to `0` if omitted.

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
        string propertyName = "_Tiling";    // required
        float  x = 2f, y = 2f, z = 0f, w = 0f;

        /* Original Logic:

            if (Validate.Required(propertyName, "propertyName") is object err) return err;

            var (material, go, error) = FindMaterial(name, instanceId, path);
            if (error != null) return error;

            if (!material.HasProperty(propertyName))
                return new {
                    error = $"Property not found: {propertyName}",
                    shaderName = material.shader.name
                };

            WorkflowManager.SnapshotObject(material);
            Undo.RecordObject(material, "Set Material Vector");
            material.SetVector(propertyName, new Vector4(x, y, z, w));

            if (go == null) EditorUtility.SetDirty(material);

            return new { success = true, target = go != null ? go.name : path, property = propertyName, value = new { x, y, z, w } };
        */
    }
}
```
