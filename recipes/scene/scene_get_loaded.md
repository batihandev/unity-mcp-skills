# scene_get_loaded

Get a list of all currently loaded scenes.

**Signature:** `SceneGetLoaded()`

**Returns:** `{ success, count, scenes: [{ name, path, isLoaded, isDirty, isActive, rootCount }] }`

No parameters. Returns every scene currently open in the editor, including additively loaded scenes. Use this instead of the non-existent `scene_list` command.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var scenes = new List<object>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            scenes.Add(new
            {
                name = scene.name,
                path = scene.path,
                isLoaded = scene.isLoaded,
                isDirty = scene.isDirty,
                isActive = scene == SceneManager.GetActiveScene(),
                rootCount = scene.rootCount
            });
        }

        result.SetResult(new { success = true, count = scenes.Count, scenes });
    }
}
```
