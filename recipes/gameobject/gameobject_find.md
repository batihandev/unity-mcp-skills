# gameobject_find

Find GameObjects matching criteria.

**Signature:** `GameObjectFind(string name = null, bool useRegex = false, string tag = null, string layer = null, string component = null, int limit = 50)`

**Returns:** `{ count, objects: [{ name, instanceId, path, tag, layer, position: { x, y, z } }] }`

## Notes

- All filters are optional and combinable. With no filters, returns up to `limit` objects from the scene.
- When `tag` is provided, the search starts from `FindGameObjectsWithTag` for efficiency before applying other filters.
- `name` uses case-insensitive substring matching by default; set `useRegex = true` to use a full regex pattern.
- `layer` accepts a layer name string (e.g., `"UI"`, `"Default"`).
- `component` accepts a component type name string (e.g., `"Rigidbody"`, `"BoxCollider"`).
- Each result includes the world `position` of the object.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md), [`component_type_finder`](../_shared/component_type_finder.md), [`skills_common`](../_shared/skills_common.md)

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = null;         // optional: substring or regex pattern
        bool useRegex = false;
        string tag = null;          // optional: e.g. "Player", "Enemy"
        string layer = null;        // optional: e.g. "UI", "Default"
        string component = null;    // optional: e.g. "Rigidbody", "AudioSource"
        int limit = 50;

        // Efficiency: If tag is provided, use FindGameObjectsWithTag (faster).
        // But we need to filter further anyway.
        IEnumerable<GameObject> results;
        if (!string.IsNullOrEmpty(tag))
            results = GameObject.FindGameObjectsWithTag(tag);
        else
            results = GameObjectFinder.GetSceneObjects();

        // Filter by Name (Regex or Contains)
        if (!string.IsNullOrEmpty(name))
        {
            if (useRegex)
            {
                var regex = new System.Text.RegularExpressions.Regex(name, System.Text.RegularExpressions.RegexOptions.None, System.TimeSpan.FromSeconds(1));
                results = results.Where(go => regex.IsMatch(go.name));
            }
            else
            {
                results = results.Where(go => go.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        // Filter by Tag (if not already fetched by tag - double check in case we fell back)
        if (!string.IsNullOrEmpty(tag))
            results = results.Where(go => go.CompareTag(tag));
    
        // Filter by Layer
        if (!string.IsNullOrEmpty(layer))
        {
            int layerId = LayerMask.NameToLayer(layer);
            if (layerId != -1)
                results = results.Where(go => go.layer == layerId);
        }

        // Filter by Component
        if (!string.IsNullOrEmpty(component))
        {
            var compType = ComponentSkills.FindComponentType(component);
    
            if (compType != null)
                results = results.Where(go => go.GetComponent(compType) != null);
        }

        var list = results.Take(limit).Select(go => new
        {
            name = go.name,
            instanceId = go.GetInstanceID(),
            path = GameObjectFinder.GetCachedPath(go),
            tag = go.tag,
            layer = LayerMask.LayerToName(go.layer),
            position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z }
        }).ToArray();

        { result.SetResult(new { count = list.Length, objects = list }); return; }
    }
}
```
