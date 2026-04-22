# validate_scene

Validate the active scene for missing scripts, missing prefabs, duplicate names, and empty GameObjects.

**Signature:** `ValidateScene(checkMissingScripts bool = true, checkMissingPrefabs bool = true, checkDuplicateNames bool = true, checkEmptyGameObjects bool = false)`

**Returns:** `{ scene, totalIssues, summary: { errors, warnings, info }, issues: [{ type, severity, gameObject, path, message, count }] }`

**Notes:**
- `checkEmptyGameObjects` is off by default to reduce noise; enable for thorough audits
- `severity` values: `"Error"`, `"Warning"`, `"Info"`

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

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
        bool checkMissingScripts = true;
        bool checkMissingPrefabs = true;
        bool checkDuplicateNames = true;
        bool checkEmptyGameObjects = false;

        var issues = new List<object>();
        var scene = SceneManager.GetActiveScene();
        var allObjects = FindHelper.FindAll<GameObject>();

        if (checkMissingScripts)
        {
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
                            severity = "Error",
                            gameObject = go.name,
                            path = GameObjectFinder.GetPath(go),
                            message = $"Missing script at component index {i}",
                            count = 0
                        });
                    }
                }
            }
        }

        if (checkMissingPrefabs)
        {
            foreach (var go in allObjects)
            {
                if (PrefabUtility.IsPrefabAssetMissing(go))
                {
                    issues.Add(new
                    {
                        type = "MissingPrefab",
                        severity = "Warning",
                        gameObject = go.name,
                        path = GameObjectFinder.GetPath(go),
                        message = "Prefab asset is missing",
                        count = 0
                    });
                }
            }
        }

        if (checkDuplicateNames)
        {
            var nameGroups = allObjects.GroupBy(go => go.name).Where(g => g.Count() > 1);
            foreach (var group in nameGroups)
            {
                issues.Add(new
                {
                    type = "DuplicateName",
                    severity = "Info",
                    gameObject = group.Key,
                    path = (string)null,
                    message = $"{group.Count()} objects share the name '{group.Key}'",
                    count = group.Count()
                });
            }
        }

        if (checkEmptyGameObjects)
        {
            foreach (var go in allObjects)
            {
                var components = go.GetComponents<Component>();
                if (components.Length == 1 && go.transform.childCount == 0)
                {
                    issues.Add(new
                    {
                        type = "EmptyGameObject",
                        severity = "Info",
                        gameObject = go.name,
                        path = GameObjectFinder.GetPath(go),
                        message = "GameObject has no components (except Transform) and no children",
                        count = 0
                    });
                }
            }
        }

        var summary = new
        {
            errors = issues.Count(i => ((dynamic)i).severity == "Error"),
            warnings = issues.Count(i => ((dynamic)i).severity == "Warning"),
            info = issues.Count(i => ((dynamic)i).severity == "Info")
        };

        result.SetResult(new
        {
            scene = scene.name,
            totalIssues = issues.Count,
            summary,
            issues
        });
    }
}
```
