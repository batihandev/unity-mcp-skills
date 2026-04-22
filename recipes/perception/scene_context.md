# scene_context

**Skill:** `scene_context`
**C# method:** `PerceptionSkills.SceneContext`

## Signature

```
SceneContext(
    int maxDepth = 10,
    int maxObjects = 200,
    string rootPath = null,
    bool includeValues = false,
    bool includeReferences = true,
    bool includeCodeDeps = false)
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `maxDepth` | `int` | `10` | Maximum hierarchy depth to traverse |
| `maxObjects` | `int` | `200` | Maximum number of objects to export |
| `rootPath` | `string` | `null` | Limit export to one subtree by object path |
| `includeValues` | `bool` | `false` | Include serialized field values |
| `includeReferences` | `bool` | `true` | Include cross-object serialized reference edges |
| `includeCodeDeps` | `bool` | `false` | Include C# code-level dependency edges |

## Return Shape

Returns `success`, `sceneName`, `totalObjects`, `scopeObjects`, `exportedObjects`, `truncated`, `objects` array, `references` array (when `includeReferences`), `codeDependencies` array (when `includeCodeDeps`).

**Prerequisites:** [`gameobject_finder`](../_shared/gameobject_finder.md)

## RunCommand Recipe

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int maxDepth = 10;
        int maxObjects = 200;
        string rootPath = null;        // e.g. "World/Environment" to scope to a subtree
        bool includeValues = false;
        bool includeReferences = true;
        bool includeCodeDeps = false;

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var totalObjects = GameObjectFinder.GetSceneObjects().Count;
        int scopeObjects;

        Transform[] roots;
        if (!string.IsNullOrEmpty(rootPath))
        {
            var rootGo = GameObjectFinder.FindByPath(rootPath);
            if (rootGo == null)
            {
                result.SetValue(new { success = false, error = $"Root path '{rootPath}' not found" });
                return;
            }
            roots = new[] { rootGo.transform };
            scopeObjects = CountSubtreeObjects(rootGo.transform);
        }
        else
        {
            roots = scene.GetRootGameObjects().Select(g => g.transform).ToArray();
            scopeObjects = totalObjects;
        }

        var objects = new List<object>();
        var references = new List<object>();
        var queue = new Queue<(Transform t, int depth)>();
        var componentBuffer = new List<Component>(8);
        var relevantUserScripts = includeCodeDeps ? new HashSet<string>() : null;
        foreach (var r in roots) queue.Enqueue((r, 0));

        while (queue.Count > 0 && objects.Count < maxObjects)
        {
            var (t, depth) = queue.Dequeue();
            objects.Add(BuildObjectInfo(t.gameObject, includeValues, includeReferences, references, componentBuffer, relevantUserScripts));

            if (depth + 1 <= maxDepth)
            {
                foreach (Transform child in t)
                    queue.Enqueue((child, depth + 1));
            }
        }

        List<object> codeDeps = null;
        if (includeCodeDeps)
        {
            codeDeps = CollectCodeDependencies(relevantUserScripts).Select(e => (object)new
            {
                from = e.fromScript,
                to = e.toObject,
                type = e.fieldType,
                detail = e.fieldName
            }).ToList();
        }

        result.SetValue(new
        {
            success = true,
            sceneName = scene.name,
            totalObjects,
            scopeObjects,
            exportedObjects = objects.Count,
            truncated = objects.Count < scopeObjects || queue.Count > 0,
            objects,
            references = includeReferences ? references : null,
            codeDependencies = codeDeps
        });
    }
}
```

## Notes

- Prefer `hierarchy_describe` when you only need a human-readable tree without component data.
- Set `includeValues = true` when AI coding steps need serialized field values.
- Set `includeCodeDeps = true` to get a rough scene-to-code dependency picture alongside object data.
- Use `rootPath` to scope large scenes to a specific subtree and avoid truncation.
