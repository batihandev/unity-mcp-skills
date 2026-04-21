# optimize_analyze_scene

Analyze the active scene for common performance bottlenecks: high-polygon meshes and GameObjects with an excessive number of material slots.

**Signature:** `OptimizeAnalyzeScene(int polyThreshold = 10000, int materialThreshold = 5)`

**Returns:** `{ success, totalRenderers, totalTriangles, totalMaterialSlots, issueCount, issues }`

- Each entry in `issues` has `type` (`"HighPoly"` or `"ExcessiveMaterials"`), `gameObject`, `path`, and either `triangles` or `materialCount`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`
- `recipes/_shared/skills_common.md` — for `SkillsCommon.*`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int polyThreshold = 10000;      // Triangle count above which a mesh is flagged
        int materialThreshold = 5;      // Material slot count above which a renderer is flagged

        var renderers = FindHelper.FindAll<Renderer>();
        var issues = new List<object>();
        int totalTris = 0, totalMats = 0;

        foreach (var r in renderers)
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                int tris = SkillsCommon.GetTriangleCount(mf.sharedMesh);
                totalTris += tris;
                if (tris > polyThreshold)
                    issues.Add(new
                    {
                        type = "HighPoly",
                        gameObject = r.name,
                        path = GameObjectFinder.GetPath(r.gameObject),
                        triangles = tris
                    });
            }

            int matCount = r.sharedMaterials.Length;
            totalMats += matCount;
            if (matCount > materialThreshold)
                issues.Add(new
                {
                    type = "ExcessiveMaterials",
                    gameObject = r.name,
                    path = GameObjectFinder.GetPath(r.gameObject),
                    materialCount = matCount
                });
        }

        result.SetResult(new
        {
            success = true,
            totalRenderers = renderers.Length,
            totalTriangles = totalTris,
            totalMaterialSlots = totalMats,
            issueCount = issues.Count,
            issues
        });
    }
}
```
