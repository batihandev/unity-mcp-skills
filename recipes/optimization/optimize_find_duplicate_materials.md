# optimize_find_duplicate_materials

Find materials that share the same shader, color, and render queue — likely candidates for consolidation. Comparison is approximate; manual review is recommended before merging materials.

**Signature:** `OptimizeFindDuplicateMaterials(int limit = 50)`

**Returns:** `{ success, duplicateGroups, groups, note }`

- `duplicateGroups` — number of duplicate groups found
- `groups` — array of `{ shader, count, paths[] }`, each group representing materials with identical key
- Key is `shader|color|renderQueue`; texture identity is not compared.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int limit = 50; // Maximum number of duplicate groups to return

        var guids = AssetDatabase.FindAssets("t:Material");
        var matInfos = new List<(string path, string key)>();

        foreach (var guid in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (mat == null || mat.shader == null) continue;

            var colorStr = mat.HasProperty("_Color")
                ? mat.color.ToString()
                : mat.HasProperty("_BaseColor")
                    ? mat.GetColor("_BaseColor").ToString()
                    : "none";

            matInfos.Add((p, mat.shader.name + "|" + colorStr + "|" + mat.renderQueue));
        }

        var duplicates = matInfos
            .GroupBy(m => m.key)
            .Where(g => g.Count() > 1)
            .Take(limit)
            .Select(g => new
            {
                shader = g.Key.Split('|')[0],
                count = g.Count(),
                paths = g.Select(m => m.path).ToArray()
            })
            .ToArray();

        result.SetResult(new
        {
            success = true,
            duplicateGroups = duplicates.Length,
            groups = duplicates,
            note = "Comparison is approximate (color/texture similarity). Manual review recommended."
        });
    }
}
```
