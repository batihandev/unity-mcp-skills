# get_scene_info

Get information about the currently active scene.

**Signature:** `GetSceneInfo()`

**Returns:** `{ sceneName, scenePath, rootObjectCount, rootObjects: [string] }`

## Notes

- Read-only — does not modify the scene.
- Returns only root-level GameObject names; use `gameobject_find` or `gameobject_get_info` for deeper inspection.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        { result.SetResult(new
        {
            sceneName = scene.name,
            scenePath = scene.path,
            rootObjectCount = roots.Length,
            rootObjects = System.Array.ConvertAll(roots, go => go.name)
        }); return; }
    }
}
```
