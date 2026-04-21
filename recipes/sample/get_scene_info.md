# get_scene_info

Get information about the currently active scene.

**Signature:** `GetSceneInfo()`

**Returns:** `{ sceneName, scenePath, rootObjectCount, rootObjects: [string] }`

## Notes

- Read-only — does not modify the scene.
- Returns only root-level GameObject names; use `gameobject_find` or `gameobject_get_info` for deeper inspection.

## Recipe

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        /* Original Logic:

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            return new
            {
                sceneName = scene.name,
                scenePath = scene.path,
                rootObjectCount = roots.Length,
                rootObjects = System.Array.ConvertAll(roots, go => go.name)
            };
        */
    }
}
```
