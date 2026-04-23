# scene_spatial_query

## Signature

```
SceneSpatialQuery(
    float x = 0, float y = 0, float z = 0,
    float radius = 10f,
    string nearObject = null,
    string componentFilter = null,
    int maxResults = 50)
```

## Return Shape

Returns `success`, `center` (x, y, z), `radius`, `totalFound`, `results` array with `name`, `path`, `distance`, `position`.

**Prerequisites:** [`gameobject_finder`](../_shared/gameobject_finder.md), [`component_type_finder`](../_shared/component_type_finder.md), [`skills_common`](../_shared/skills_common.md)

```csharp
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        float x = 0f, y = 0f, z = 0f;
        float radius = 10f;
        string nearObject = null;       // e.g. "Player" to search around that object
        string componentFilter = null;  // e.g. "Rigidbody" to find only physics objects
        int maxResults = 50;

        Vector3 center;

        if (!string.IsNullOrEmpty(nearObject))
        {
            var go = GameObjectFinder.Find(nearObject);
            if (go == null)
            {
                result.SetResult(new { success = false, error = $"Object '{nearObject}' not found" });
                return;
            }
            center = go.transform.position;
        }
        else
        {
            center = new Vector3(x, y, z);
        }

        var allObjects = GameObjectFinder.GetSceneObjects();
        float radiusSq = radius * radius;

        Type filterType = string.IsNullOrEmpty(componentFilter)
            ? null
            : ComponentSkills.FindComponentType(componentFilter);

        var found = new List<(float dist, object info)>();
        foreach (var go in allObjects)
        {
            if (filterType != null && go.GetComponent(filterType) == null) continue;

            var pos = go.transform.position;
            float distSq = (pos - center).sqrMagnitude;
            if (distSq <= radiusSq)
            {
                float dist = Mathf.Sqrt(distSq);
                found.Add((dist, new
                {
                    name = go.name,
                    path = GameObjectFinder.GetCachedPath(go),
                    distance = dist,
                    position = new { x = pos.x, y = pos.y, z = pos.z }
                }));
            }
        }

        var results = found.Count <= maxResults
            ? found.Select(f => f.info).ToList()
            : found.OrderBy(f => f.dist).Take(maxResults).Select(f => f.info).ToList();

        result.SetResult(new
        {
            success = true,
            center = new { x = center.x, y = center.y, z = center.z },
            radius,
            totalFound = found.Count,
            results
        });
    }
}
```

## Notes

- When `totalFound > maxResults`, the closest objects are returned.
- `componentFilter` uses the same component type resolution as other component skills.
- Useful for proximity checks (e.g. "find all enemies within 15 units of the spawn point").
