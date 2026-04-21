# material_set_int

Set an integer property on a material.

**Signature:** `MaterialSetInt(string name = null, int instanceId = 0, string path = null, string propertyName = null, int value = 0)`

**Returns:** `{ success, target, property, value }`

## Notes

- `propertyName` is required. Returns an error (with `shaderName`) if the property does not exist on the shader.
- Common integer properties include surface type toggles and stencil values.
- Use `material_get_properties` to discover integer properties available on a material.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name         = "Cube";          // target GameObject name
        int    instanceId   = 0;
        string path         = null;            // or material asset path
        string propertyName = "_Surface";      // required (0=Opaque, 1=Transparent in URP)
        int    value        = 1;

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
            Undo.RecordObject(material, "Set Material Int");
            material.SetInt(propertyName, value);

            if (go == null) EditorUtility.SetDirty(material);

            return new { success = true, target = go != null ? go.name : path, property = propertyName, value };
        */
    }
}
```
