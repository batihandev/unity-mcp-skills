# scene_get_hierarchy

Get the full scene hierarchy tree with component names at each node.

**Signature:** `SceneGetHierarchy(int maxDepth = 3)`

**Returns:** `{ sceneName, hierarchy: [{ name, instanceId, components: [string], children: [...] }] }`

`maxDepth` controls how many levels deep the tree expands. Nodes at the depth limit have `children: null`. For a narrative description of the hierarchy use `perception` module's `hierarchy_describe` instead.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int maxDepth = 3; // Maximum hierarchy depth to traverse (default 3)

        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var hierarchy = new object[roots.Length];
        var componentBuffer = new List<Component>(8);

        for (int i = 0; i < roots.Length; i++)
            hierarchy[i] = GetHierarchyNode(roots[i], 0, maxDepth, componentBuffer);

        result.SetResult(new { sceneName = scene.name, hierarchy });
    }

    private object GetHierarchyNode(GameObject go, int depth, int maxDepth, List<Component> componentBuffer)
    {
        var childCount = go.transform.childCount;
        object[] children = null;
        if (depth < maxDepth && childCount > 0)
        {
            children = new object[childCount];
            for (int i = 0; i < childCount; i++)
                children[i] = GetHierarchyNode(go.transform.GetChild(i).gameObject, depth + 1, maxDepth, componentBuffer);
        }

        return new
        {
            name = go.name,
            instanceId = go.GetInstanceID(),
            components = GetComponentTypeNames(go, componentBuffer),
            children
        };
    }

    private string[] GetComponentTypeNames(GameObject go, List<Component> componentBuffer)
    {
        componentBuffer.Clear();
        go.GetComponents(componentBuffer);
        var names = new List<string>(componentBuffer.Count);
        foreach (var c in componentBuffer)
            if (c != null) names.Add(c.GetType().Name);
        return names.ToArray();
    }
}
```
