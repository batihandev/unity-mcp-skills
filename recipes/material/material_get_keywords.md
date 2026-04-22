# material_get_keywords

Get all enabled shader keywords on a material, plus the status of common known keywords.

**Signature:** `MaterialGetKeywords(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, target, shader, enabledKeywords, commonKeywordStatus: [{ keyword, enabled }] }`

## Notes

- Read-only: does not modify the material.
- `enabledKeywords` is the raw array of currently-enabled keywords.
- `commonKeywordStatus` checks a fixed set of well-known keywords (see list below) and reports their enabled/disabled state regardless of whether they are in `enabledKeywords`.
- Use `material_set_keyword` to enable or disable individual keywords.

## Common Keywords Checked

`_EMISSION`, `_NORMALMAP`, `_METALLICGLOSSMAP`, `_SPECGLOSSMAP`, `_ALPHATEST_ON`, `_ALPHABLEND_ON`, `_ALPHAPREMULTIPLY_ON`, `_DETAIL_MULX2`, `_PARALLAXMAP`, `_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A`, `_SPECULARHIGHLIGHTS_OFF`, `_ENVIRONMENTREFLECTIONS_OFF`, `_RECEIVE_SHADOWS_OFF`, `_SURFACE_TYPE_TRANSPARENT`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name       = "Cube"; // target GameObject name
        int    instanceId = 0;
        string path       = null;   // or material asset path

        var (material, go, error) = FindMaterial(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        // Get common keywords that might be available
        var commonKeywords = new[] {
            "_EMISSION", "_NORMALMAP", "_METALLICGLOSSMAP", "_SPECGLOSSMAP",
            "_ALPHATEST_ON", "_ALPHABLEND_ON", "_ALPHAPREMULTIPLY_ON",
            "_DETAIL_MULX2", "_PARALLAXMAP", "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
            "_SPECULARHIGHLIGHTS_OFF", "_ENVIRONMENTREFLECTIONS_OFF",
            "_RECEIVE_SHADOWS_OFF", "_SURFACE_TYPE_TRANSPARENT"
        };

        var enabledKeywords = material.shaderKeywords;
        var keywordStatus = new List<object>();

        foreach (var kw in commonKeywords)
        {
            keywordStatus.Add(new { keyword = kw, enabled = material.IsKeywordEnabled(kw) });
        }

        { result.SetResult(new {
            success = true,
            target = go != null ? go.name : path,
            shader = material.shader.name,
            enabledKeywords,
            commonKeywordStatus = keywordStatus
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
