# uitk_inspect_document

Inspect the live VisualElement tree of a UIDocument in the active scene.

**Signature:** `UitkInspectDocument(name string = null, instanceId int = 0, path string = null, depth int = 5)`

**Returns:** `{ gameObject, instanceId, hierarchy }`

**Notes:**
- Identify the target with exactly one of `name`, `instanceId`, or `path`.
- The document must be active in the scene for `rootVisualElement` to be available.
- `depth` controls how many levels deep the hierarchy is traversed.

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string name = "UIDocument";
        int instanceId = 0;
        string path = null;
        int depth = 5;

        var go = GameObjectFinder.Find(name, instanceId, path);
        if (go == null) { result.SetResult(new { error = $"GameObject not found: {name ?? path}" }); return; }

        var doc = go.GetComponent<UIDocument>();
        if (doc == null) { result.SetResult(new { error = $"No UIDocument component on '{go.name}'" }); return; }

        var root = doc.rootVisualElement;
        if (root == null) { result.SetResult(new { error = "UIDocument has no rootVisualElement (document may not be active)" }); return; }

        var hierarchy = InspectVisualElement(root, 0, depth);
        result.SetResult(new
        {
            gameObject = go.name,
            instanceId = go.GetInstanceID(),
            hierarchy
        });
    }

    private object InspectVisualElement(VisualElement ve, int currentDepth, int maxDepth)
    {
        var tag = ve.GetType().Name;
        var attrs = new System.Collections.Generic.Dictionary<string, string>
        {
            ["name"]    = ve.name,
            ["classes"] = string.Join(" ", ve.GetClasses())
        };

        if (currentDepth >= maxDepth && ve.childCount > 0)
            return new { tag, attributes = attrs, children = new[] { new { note = $"[{ve.childCount} children; truncated at depth {maxDepth}]" } } };

        var children = new System.Collections.Generic.List<object>();
        foreach (var child in ve.Children())
            children.Add(InspectVisualElement(child, currentDepth + 1, maxDepth));

        return new { tag, attributes = attrs, children = children.ToArray() };
    }
}
```
