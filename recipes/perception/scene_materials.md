# scene_materials

**Skill:** `scene_materials`
**C# method:** `PerceptionSkills.SceneMaterials`

## Signature

```
SceneMaterials(bool includeProperties = false)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `includeProperties` | `bool` | `false` | Whether to include per-material shader property list |

## Return Shape

Returns `success`, `totalMaterials`, `totalShaders`, `shaders` array grouped by shader name — each entry has `shader`, `materialCount`, `materials` (name, path, renderQueue, userCount, users[0..4], properties when requested).

**Prerequisites:** [`gameobject_finder`](../_shared/gameobject_finder.md)

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
        bool includeProperties = false;

        var renderers = FindHelper.FindAll<Renderer>();
        var materialMap = new Dictionary<string, MaterialInfo>();

        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null) continue;
                var key = mat.GetInstanceID().ToString();
                if (!materialMap.ContainsKey(key))
                {
                    materialMap[key] = new MaterialInfo
                    {
                        name = mat.name,
                        shader = mat.shader != null ? mat.shader.name : "null",
                        renderQueue = mat.renderQueue,
                        path = AssetDatabase.GetAssetPath(mat),
                        users = new List<string>()
                    };
                    if (includeProperties && mat.shader != null)
                    {
                        var props = new List<object>();
                        int count = ShaderUtil.GetPropertyCount(mat.shader);
                        for (int i = 0; i < count; i++)
                        {
                            props.Add(new
                            {
                                name = ShaderUtil.GetPropertyName(mat.shader, i),
                                type = ShaderUtil.GetPropertyType(mat.shader, i).ToString()
                            });
                        }
                        materialMap[key].properties = props;
                    }
                }
                materialMap[key].users.Add(renderer.gameObject.name);
            }
        }

        var shaderGroups = materialMap.Values
            .GroupBy(m => m.shader)
            .Select(g => new
            {
                shader = g.Key,
                materialCount = g.Count(),
                materials = g.Select(m => new
                {
                    m.name, m.path, m.renderQueue,
                    userCount = m.users.Count,
                    users = m.users.Take(5).ToList(),
                    properties = includeProperties ? m.properties : null
                }).ToList()
            })
            .OrderByDescending(g => g.materialCount)
            .ToList();

        result.SetValue(new
        {
            success = true,
            totalMaterials = materialMap.Count,
            totalShaders = shaderGroups.Count,
            shaders = shaderGroups
        });
    }
}
```

## Notes

- Groups results by shader to make it easy to spot shader variety or redundancy.
- `users` is capped at 5 per material in the recipe output.
- Use `project_stack_detect` to determine the render pipeline before interpreting shader names.
