# validate_find_missing_scripts

Find all GameObjects in the scene (and optionally prefab assets) that have missing script references.

**Signature:** `ValidateFindMissingScripts(searchInPrefabs bool = true)`

**Returns:** `{ totalFound, objects: [{ source, gameObject, path, missingCount }] }`

**Notes:**
- `source` is either `"Scene"` or `"Prefab"`
- Prefab objects include a `prefabPath` field instead of `path`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool searchInPrefabs = true;

        var results = new List<object>();

        var sceneObjects = FindHelper.FindAll<GameObject>();
        foreach (var go in sceneObjects)
        {
            var components = go.GetComponents<Component>();
            var missingCount = components.Count(c => c == null);
            if (missingCount > 0)
            {
                results.Add(new
                {
                    source = "Scene",
                    gameObject = go.name,
                    path = GameObjectFinder.GetPath(go),
                    missingCount
                });
            }
        }

        if (searchInPrefabs)
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var allChildren = prefab.GetComponentsInChildren<Transform>(true);
                foreach (var t in allChildren)
                {
                    var components = t.gameObject.GetComponents<Component>();
                    var missingCount = components.Count(c => c == null);
                    if (missingCount > 0)
                    {
                        results.Add(new
                        {
                            source = "Prefab",
                            prefabPath = path,
                            gameObject = t.name,
                            missingCount
                        });
                    }
                }
            }
        }

        result.SetResult(new { totalFound = results.Count, objects = results });
    }
}
```
