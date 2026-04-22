# optimize_analyze_overdraw

Find renderers in the active scene whose materials use a render queue of 2500 or higher (transparent/alpha-blended), which are common sources of GPU overdraw.

**Signature:** `OptimizeAnalyzeOverdraw(int limit = 50)`

**Returns:** `{ success, transparentObjectCount, objects }`

- `objects` — array of `{ gameObject, path, material, renderQueue, shader }`, one entry per renderer (the first transparent material on the renderer triggers the entry).

**Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`gameobject_finder`](../_shared/gameobject_finder.md)

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int limit = 50; // Maximum number of results to return

        var renderers = FindHelper.FindAll<Renderer>();
        var transparent = new List<object>();

        foreach (var r in renderers)
        {
            if (transparent.Count >= limit) break;

            foreach (var mat in r.sharedMaterials)
            {
                if (mat != null && mat.renderQueue >= 2500)
                {
                    transparent.Add(new
                    {
                        gameObject = r.name,
                        path = GameObjectFinder.GetPath(r.gameObject),
                        material = mat.name,
                        renderQueue = mat.renderQueue,
                        shader = mat.shader != null ? mat.shader.name : "null"
                    });
                    break; // One entry per renderer
                }
            }
        }

        result.SetResult(new
        {
            success = true,
            transparentObjectCount = transparent.Count,
            objects = transparent
        });
    }
}
```
