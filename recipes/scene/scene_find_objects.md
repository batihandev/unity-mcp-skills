# scene_find_objects

Search GameObjects in the scene by name pattern, tag, or component type.

**Signature:** `SceneFindObjects(string namePattern = null, string tag = null, string componentType = null, int limit = 50)`

**Returns:** `{ success, count, objects: [{ name, path, instanceId, active, tag }] }`

All filter parameters are optional and combinable. `namePattern` is a case-insensitive substring match. For regex, layer, or path-based search use `gameobject_find` (Full-Auto) instead.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string namePattern = null;     // Case-insensitive substring match on GameObject name (null = no filter)
        string tag = null;             // Filter by tag (null = no filter)
        string componentType = null;   // Filter by component type name, e.g. "Rigidbody" (null = no filter)
        int limit = 50;                // Max results to return

        IEnumerable<GameObject> objects = GetSceneObjects();

        if (!string.IsNullOrEmpty(tag))
        {
            try { objects = objects.Where(go => go.CompareTag(tag)); }
            catch { result.SetResult(new { error = $"Invalid tag: {tag}" }); return; }
        }

        if (!string.IsNullOrEmpty(namePattern))
            objects = objects.Where(go => go.name.IndexOf(namePattern, System.StringComparison.OrdinalIgnoreCase) >= 0);

        if (!string.IsNullOrEmpty(componentType))
        {
            var type = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return System.Array.Empty<System.Type>(); } })
                .FirstOrDefault(t => t.Name == componentType && typeof(Component).IsAssignableFrom(t));
            if (type == null) { result.SetResult(new { error = $"Component type not found: {componentType}" }); return; }
            objects = objects.Where(go => go.GetComponent(type) != null);
        }

        var results = objects.Take(limit).Select(go => new
        {
            name = go.name,
            path = GetPath(go),
            instanceId = go.GetInstanceID(),
            active = go.activeInHierarchy,
            tag = go.tag
        }).ToArray();

        result.SetResult(new { success = true, count = results.Length, objects = results });
    }

    private static IEnumerable<GameObject> GetSceneObjects()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(t => t.gameObject);
    }

    private static string GetPath(GameObject go)
    {
        var parts = new List<string>();
        var t = go.transform;
        while (t != null) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join("/", parts);
    }
}
```
