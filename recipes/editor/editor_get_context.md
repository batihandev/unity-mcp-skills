# editor_get_context

Get full editor context: selected GameObjects, selected Project-window assets, active scene info, and focused window. Use this to read the current selection without a separate search call.

**Signature:** `EditorGetContext(bool includeComponents = false, bool includeChildren = false)`

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `includeComponents` | bool | false | Attach component name list to each selected GameObject |
| `includeChildren` | bool | false | Attach direct child list to each selected GameObject |

**Returns:**

```
{
  success,
  selectedGameObjects: { count, objects: [{ name, instanceId, path, tag, layer, isActive, [components], [children] }] },
  selectedAssets:      { count, assets:  [{ guid, path, type, isFolder }] },
  activeScene:         { name, path, isDirty },
  focusedWindow,       // editor window class name or "None"
  isPlaying,
  isCompiling
}
```

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEditor;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool includeComponents = false;
        bool includeChildren = false;

        // 1. Hierarchy selected GameObjects
        var selectedGameObjects = Selection.gameObjects.Select(go =>
        {
            var info = new System.Collections.Generic.Dictionary<string, object>
            {
                ["name"]       = go.name,
                ["instanceId"] = go.GetInstanceID(),
                ["path"]       = GameObjectFinder.GetPath(go),
                ["tag"]        = go.tag,
                ["layer"]      = LayerMask.LayerToName(go.layer),
                ["isActive"]   = go.activeSelf
            };

            if (includeComponents)
            {
                info["components"] = go.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .ToArray();
            }

            if (includeChildren && go.transform.childCount > 0)
            {
                var children = new System.Collections.Generic.List<object>();
                foreach (Transform child in go.transform)
                    children.Add(new { name = child.name, instanceId = child.gameObject.GetInstanceID() });
                info["children"] = children;
            }

            return info;
        }).ToArray();

        // 2. Project-window selected assets (via GUID)
        var selectedAssets = Selection.assetGUIDs.Select(guid =>
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            return new
            {
                guid,
                path,
                type     = assetType?.Name ?? "Unknown",
                isFolder = AssetDatabase.IsValidFolder(path)
            };
        }).ToArray();

        // 3. Active scene
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        // 4. Focused window
        var focusedWindow = EditorWindow.focusedWindow;

        result.SetResult(new
        {
            success             = true,
            selectedGameObjects = new { count = selectedGameObjects.Length, objects = selectedGameObjects },
            selectedAssets      = new { count = selectedAssets.Length,      assets  = selectedAssets },
            activeScene         = new { name = activeScene.name, path = activeScene.path, isDirty = activeScene.isDirty },
            focusedWindow       = focusedWindow?.GetType().Name ?? "None",
            isPlaying           = EditorApplication.isPlaying,
            isCompiling         = EditorApplication.isCompiling
        });
    }
}
```
