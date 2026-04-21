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

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name       = "Cube"; // target GameObject name
        int    instanceId = 0;
        string path       = null;   // or material asset path

        /* Original Logic:

            var (material, go, error) = FindMaterial(name, instanceId, path);
            if (error != null) return error;

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
                keywordStatus.Add(new { keyword = kw, enabled = material.IsKeywordEnabled(kw) });

            return new {
                success = true,
                target = go != null ? go.name : path,
                shader = material.shader.name,
                enabledKeywords,
                commonKeywordStatus = keywordStatus
            };
        */
    }
}
```
