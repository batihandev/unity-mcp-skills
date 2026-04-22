# cleaner_find_missing_references

Find components with missing scripts or null serialized object references in the current scene's loaded GameObjects.

**Signature:** `CleanerFindMissingReferences(bool includeInactive = true)`

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `includeInactive` | bool | No | true | Include inactive GameObjects in the search |

## Returns

```json
{
  "success": true,
  "issueCount": 4,
  "missingScripts": 2,
  "missingReferences": 2,
  "issues": [
    {
      "type": "MissingScript",
      "gameObject": "Player",
      "path": "Player",
      "componentIndex": 1
    },
    {
      "type": "MissingReference",
      "gameObject": "Enemy",
      "path": "Enemy",
      "component": "EnemyController",
      "property": "targetTransform"
    }
  ]
}
```

## Issue Types

| Type | Cause | Fields |
|------|-------|--------|
| `MissingScript` | Script DLL removed or GUID broken | `gameObject`, `path`, `componentIndex` |
| `MissingReference` | Asset deleted but serialized reference remains | `gameObject`, `path`, `component`, `property` |

## Notes

- Operates on loaded scene objects only. Objects in unloaded scenes are not scanned.
- When `includeInactive = true`, uses `Resources.FindObjectsOfTypeAll` which finds objects with `HideFlags.None` that are not persistent assets.
- Use `cleaner_fix_missing_scripts` to automatically remove MissingScript components.
- MissingReference issues must be fixed manually by reassigning the field in the Inspector.

## C# Template

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool includeInactive = true;

        var issues = new List<object>();
        var allObjects = includeInactive
            ? Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => !EditorUtility.IsPersistent(go) && go.hideFlags == HideFlags.None)
                .ToArray()
            : Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (var go in allObjects)
        {
            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    issues.Add(new
                    {
                        type = "MissingScript",
                        gameObject = go.name,
                        path = AnimationUtility.CalculateTransformPath(go.transform, null),
                        componentIndex = i
                    });
                }
            }

            foreach (var component in components.Where(c => c != null))
            {
                var so = new SerializedObject(component);
                var prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                        {
                            issues.Add(new
                            {
                                type = "MissingReference",
                                gameObject = go.name,
                                path = AnimationUtility.CalculateTransformPath(go.transform, null),
                                component = component.GetType().Name,
                                property = prop.propertyPath
                            });
                        }
                    }
                }
            }
        }

        result.SetValue(new
        {
            success = true,
            issueCount = issues.Count,
            missingScripts = issues.Count(i => ((dynamic)i).type == "MissingScript"),
            missingReferences = issues.Count(i => ((dynamic)i).type == "MissingReference"),
            issues
        });
    }
}
```
