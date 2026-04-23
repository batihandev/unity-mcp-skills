# material_set_float

Set a float property on a material.

**Signature:** `MaterialSetFloat(string name = null, int instanceId = 0, string path = null, string propertyName = null, float value = 0)`

**Returns:** `{ success, target, property, value }`

## Notes
- Use this to set `_Metallic`, `_Glossiness` (Standard), `_Smoothness` (URP), `_Cutoff`, or any other float property.
- Use `material_get_properties` to discover available float properties and their valid ranges.
- There is no `material_set_metallic` or `material_set_smoothness` skill — use this with the appropriate `propertyName`.

## Common Property Names

| Pipeline | Metallic | Smoothness |
|----------|----------|------------|
| Standard | `_Metallic` | `_Glossiness` |
| URP | `_Metallic` | `_Smoothness` |
| HDRP | `_Metallic` | `_Smoothness` |

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`validate`](../_shared/validate.md), [`workflow_manager`](../_shared/workflow_manager.md)

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

        if (Validate.Required(propertyName, "propertyName") is object err) { result.SetResult(err); return; }

        var (material, go, error) = FindMaterial(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        if (!material.HasProperty(propertyName))
        {
            { result.SetResult(new { 
                error = $"Property not found: {propertyName}",
                shaderName = material.shader.name,
                suggestion = "Use material_get_properties to see available properties"
            }); return; }
        }

        WorkflowManager.SnapshotObject(material);
        Undo.RecordObject(material, "Set Material Float");
        material.SetFloat(propertyName, value);

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
