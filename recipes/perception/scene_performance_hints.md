# scene_performance_hints

**Skill:** `scene_performance_hints`
**C# method:** `PerceptionSkills.ScenePerformanceHints`

## Signature

```
ScenePerformanceHints()
```

## Parameters

None.

## Return Shape

Returns `success`, `hintCount`, `hints` array with `priority` (1=high, 3=low), `category`, `issue`, `suggestion`, `fixSkill` (name of the skill to fix the issue, or null).

**Prerequisites:** [`gameobject_finder`](../_shared/gameobject_finder.md), [`skills_common`](../_shared/skills_common.md)

## RunCommand Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var hints = new List<object>();

        // 1. Realtime shadow lights
        var lights = FindHelper.FindAll<Light>();
        var shadowLights = lights.Where(l => l.shadows != LightShadows.None).ToArray();
        if (shadowLights.Length > 4)
            hints.Add(new { priority = 1, category = "Lighting", issue = $"{shadowLights.Length} shadow-casting lights",
                suggestion = "Reduce to ≤4 or use baked lighting", fixSkill = "light_set_properties" });

        // 2. Non-static renderers
        var renderers = FindHelper.FindAll<Renderer>();
        int nonStaticCount = renderers.Count(r => !r.gameObject.isStatic);
        if (nonStaticCount > 100)
            hints.Add(new { priority = 2, category = "Batching", issue = $"{nonStaticCount} non-static renderers",
                suggestion = "Mark static objects with optimize_set_static_flags", fixSkill = "optimize_set_static_flags" });

        // 3. High-poly meshes without LOD
        var meshFilters = FindHelper.FindAll<MeshFilter>();
        var highPoly = meshFilters.Where(mf => mf.sharedMesh != null && SkillsCommon.GetTriangleCount(mf.sharedMesh) > 10000
            && mf.GetComponent<LODGroup>() == null).ToArray();
        if (highPoly.Length > 0)
            hints.Add(new { priority = 2, category = "Geometry", issue = $"{highPoly.Length} high-poly meshes (>10k tris) without LOD",
                suggestion = "Add LOD groups", fixSkill = "optimize_set_lod_group" });

        // 4. Duplicate materials
        var mats = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).ToArray();
        var duplicateCount = mats.Length - mats.Select(m => m.GetInstanceID()).Distinct().Count();
        if (duplicateCount > 10)
            hints.Add(new { priority = 3, category = "Materials", issue = $"{duplicateCount} duplicate material references",
                suggestion = "Consolidate materials", fixSkill = "optimize_find_duplicate_materials" });

        // 5. Particle systems
        var particles = FindHelper.FindAll<ParticleSystem>();
        if (particles.Length > 20)
            hints.Add(new { priority = 3, category = "Particles", issue = $"{particles.Length} particle systems",
                suggestion = "Consider reducing or pooling particle systems", fixSkill = (string)null });

        if (hints.Count == 0)
            hints.Add(new { priority = 0, category = "OK", issue = "No obvious performance issues",
                suggestion = "Scene looks good", fixSkill = (string)null });

        result.SetResult(new { success = true, hintCount = hints.Count, hints });
    }
}
```

## Notes

- Thresholds: >4 shadow lights (priority 1), >100 non-static renderers (priority 2), any high-poly meshes without LOD (priority 2), >10 duplicate materials (priority 3), >20 particle systems (priority 3).
- `fixSkill` provides the recommended skill to address each hint — pass it directly to the next tool call.
- When all checks pass, a single `priority=0 / category="OK"` entry is returned.
