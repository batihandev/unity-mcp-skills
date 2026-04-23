# material_set_emission

Set emission color with HDR intensity and auto-enable the emission keyword.

**Signature:** `MaterialSetEmission(string name = null, int instanceId = 0, string path = null, float r = 1, float g = 1, float b = 1, float intensity = 1.0f, bool enableEmission = true)`

**Returns:** `{ success, target, emissionColor: {r,g,b}, intensity, hdrColor: {r,g,b}, emissionEnabled }`

## Notes

- The HDR color stored on the material is `(r * intensity, g * intensity, b * intensity)`. Set `intensity > 1` for visible bloom in post-processing.
- Tries `_EmissionColor` then `_Emission` for the property name; returns an error if neither exists on the shader.
- When `enableEmission = true` and `intensity > 0`: enables the `_EMISSION` keyword and sets `globalIlluminationFlags = RealtimeEmissive`.
- When `enableEmission = false` or `intensity <= 0`: disables the keyword and sets `globalIlluminationFlags = EmissiveIsBlack`.
- There is no `alpha` parameter — emission alpha is always set to 1.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`workflow_manager`](../_shared/workflow_manager.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name            = "Lantern";  // target GameObject name
        int    instanceId      = 0;
        string path            = null;       // or material asset path
        float  r = 1f, g = 0.8f, b = 0.2f; // warm yellow-white; values 0-1
        float  intensity       = 3.0f;      // >1 for HDR bloom
        bool   enableEmission  = true;      // auto-enable _EMISSION keyword

        var (material, go, error) = FindMaterial(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        WorkflowManager.SnapshotObject(material);
        Undo.RecordObject(material, "Set Material Emission");

        // Calculate HDR color
        var hdrColor = new Color(r * intensity, g * intensity, b * intensity, 1f);

        // Try emission property names
        string emissionProperty = null;
        var emissionProps = new[] { "_EmissionColor", "_Emission" };
        foreach (var prop in emissionProps)
        {
            if (material.HasProperty(prop))
            {
                material.SetColor(prop, hdrColor);
                emissionProperty = prop;
                break;
            }
        }

        if (emissionProperty == null)
        {
            { result.SetResult(new { 
                error = "Material does not support emission",
                shaderName = material.shader.name,
                suggestion = "Use a shader that supports emission like Standard, URP/Lit, or HDRP/Lit"
            }); return; }
        }

        // Enable emission
        if (enableEmission && intensity > 0)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else if (!enableEmission || intensity <= 0)
        {
            material.DisableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        if (go == null) EditorUtility.SetDirty(material);

        { result.SetResult(new {
            success = true,
            target = go != null ? go.name : path,
            emissionColor = new { r, g, b },
            intensity,
            hdrColor = new { r = hdrColor.r, g = hdrColor.g, b = hdrColor.b },
            emissionEnabled = enableEmission && intensity > 0
        }); return; }
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
