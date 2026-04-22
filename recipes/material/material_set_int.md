# material_set_int

Set an integer property on a material.

**Signature:** `MaterialSetInt(string name = null, int instanceId = 0, string path = null, string propertyName = null, int value = 0)`

**Returns:** `{ success, target, property, value }`

## Notes

- `propertyName` is required. Returns an error (with `shaderName`) if the property does not exist on the shader.
- Common integer properties include surface type toggles and stencil values.
- Use `material_get_properties` to discover integer properties available on a material.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

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

        if (Validate.Required(propertyName, "propertyName") is object err) { result.SetResult(err); return; }

        var (material, go, error) = FindMaterial(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        if (!material.HasProperty(propertyName))
        {
            { result.SetResult(new {
                error = $"Property not found: {propertyName}",
                shaderName = material.shader.name
            }); return; }
        }

        WorkflowManager.SnapshotObject(material);
        Undo.RecordObject(material, "Set Material Int");
        material.SetInt(propertyName, value);

        if (go == null) EditorUtility.SetDirty(material);

        { result.SetResult(new { success = true, target = go != null ? go.name : path, property = propertyName, value }); return; }
    }

    private static (Material mat, GameObject go, object error) FindMaterial(string name, int instanceId, string path)
    {
        if (!string.IsNullOrEmpty(path) && path.EndsWith(".mat"))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) return (null, null, new { error = "Material asset not found: " + path });
            return (m, null, null);
        }
        var (go, err) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (err != null) return (null, null, err);
        var rdr = go.GetComponent<Renderer>();
        if (rdr == null) return (null, go, new { error = "No Renderer on " + go.name });
        var mat = rdr.sharedMaterial;
        if (mat == null) return (null, go, new { error = "No material on " + go.name });
        return (mat, go, null);
    }
}
```
