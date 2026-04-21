# find_objects_by_name

Find all GameObjects whose name contains the given string (case-insensitive).

**Signature:** `FindObjectsByName(string nameContains = null, string name = null)`

**Returns:** `{ query, count, objects: [{ name, position: { x, y, z } }] }`

## Notes

- `name` is an accepted alias for `nameContains` for compatibility.
- Matching is case-insensitive and substring-based.
- Read-only — does not modify the scene.
- Prefer `gameobject_find` for richer filtering (tag, layer, component).

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string nameContains = "Cube"; // required (or use `name` alias)
        string name = null; // optional alias for nameContains

        /* Original Logic:

            nameContains = nameContains ?? name;
            if (Validate.Required(nameContains, "nameContains") is object err) return err;

            var allObjects = FindHelper.FindAll<GameObject>();
            var matches = System.Array.FindAll(allObjects,
                go => go != null && go.name.IndexOf(nameContains, System.StringComparison.OrdinalIgnoreCase) >= 0);
            return new
            {
                query = nameContains,
                count = matches.Length,
                objects = System.Array.ConvertAll(matches, go => new
                {
                    name = go.name,
                    position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z }
                })
            };
        */
    }
}
```
