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
        int errCount = 0, warnCount = 0, infoCount = 0;

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
                        errCount++;
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
                    warnCount++;
                }
            }
        }

        if (checkDuplicateNames)
        {
            var nameCounts = new Dictionary<string, int>();
            foreach (var go in allObjects)
            {
                if (nameCounts.ContainsKey(go.name)) nameCounts[go.name]++;
                else nameCounts[go.name] = 1;
            }
            foreach (var kv in nameCounts)
            {
                if (kv.Value <= 1) continue;
                issues.Add(new
                {
                    type = "DuplicateName",
                    severity = "Info",
                    gameObject = kv.Key,
                    path = (string)null,
                    message = $"{kv.Value} objects share the name '{kv.Key}'",
                    count = kv.Value
                });
                infoCount++;
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
                    infoCount++;
                }
            }
        }

        var summary = new { errors = errCount, warnings = warnCount, info = infoCount };

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
