# validate_missing_references

Find null/missing object references on components in the active scene.

**Signature:** `ValidateMissingReferences(limit int = 50)`

**Returns:** `{ success, count, issues: [{ gameObject, path, component, property }] }`

**Notes:**
- Detects serialized fields that previously pointed to an object (non-zero instance ID) but whose reference is now null
- Only scans scene objects; does not scan prefab assets on disk
- At most one issue per component per GameObject is reported (breaks after first missing field found on each component)

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int limit = 50;

        var results = new List<object>();
        foreach (var go in FindHelper.FindAll<GameObject>())
        {
            if (results.Count >= limit) break;
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                var so = new SerializedObject(comp);
                var prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                        prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                    {
                        results.Add(new
                        {
                            gameObject = go.name,
                            path = GameObjectFinder.GetPath(go),
                            component = comp.GetType().Name,
                            property = prop.propertyPath
                        });
                        break;
                    }
                }
            }
        }

        result.SetResult(new { success = true, count = results.Count, issues = results });
    }
}
```
