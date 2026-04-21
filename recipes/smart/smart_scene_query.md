# smart_scene_query

Query scene objects by component property values (SQL-like filter).

**Signature:** `SmartSceneQuery(string componentName = null, string propertyName = null, string op = "==", string value = null, int limit = 50, string query = null)`

**Returns:** `{ success, count, query, results: [{ name, instanceId, path, propertyValue }] }`

**Notes:**
- `op` values: `==`, `!=`, `>`, `<`, `>=`, `<=`, `contains`
- `query` shorthand is not supported; always use `componentName`/`propertyName`/`op`/`value`.
- `componentName` and `propertyName` are required.
- Numeric comparison uses a tolerance of 0.0001 for `==`/`!=`.
- Read-only; no undo entry is created.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string componentName = "Light";       // e.g. Light, Camera, MeshRenderer
        string propertyName = "intensity";    // property or field name
        string op = ">";                      // ==, !=, >, <, >=, <=, contains
        string value = "2";
        int limit = 50;

        if (string.IsNullOrWhiteSpace(componentName) && !string.IsNullOrWhiteSpace(query))
        {
            { result.SetResult(new
            {
                success = false,
                error = "query shorthand is not supported. Use componentName/propertyName/op/value, e.g. componentName='Light', propertyName='intensity', op='>', value='2'."
            }); return; }
        }

        if (Validate.Required(componentName, "componentName") is object componentErr) { result.SetResult(componentErr); return; }
        if (Validate.Required(propertyName, "propertyName") is object propertyErr) { result.SetResult(propertyErr); return; }

        var results = new List<object>();

        // Resolve Type
        var type = GetTypeByName(componentName);
        if (type == null) 
            { result.SetResult(new { success = false, error = $"Component type '{componentName}' not found. Try: Light, MeshRenderer, Camera, etc." }); return; }

        var components = Object.FindObjectsOfType(type);

        foreach (var comp in components)
        {
            if (results.Count >= limit) break;

            var val = GetMemberValue(comp, propertyName);
            if (val == null) continue;

            if (Compare(val, op, value))
            {
                var go = (comp is Component c) ? c.gameObject : null;
                if (go == null) continue;
                results.Add(new 
                {
                    name = go.name,
                    instanceId = go.GetInstanceID(),
                    path = GameObjectFinder.GetPath(go),
                    propertyValue = FormatValue(val)
                });
            }
        }

        { result.SetResult(new 
        { 
            success = true, 
            count = results.Count, 
            query = $"{componentName}.{propertyName} {op} {value}",
            results
        }); return; }
    }
}
```
