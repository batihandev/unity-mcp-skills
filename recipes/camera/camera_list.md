# camera_list

List all Camera components in the current scene, ordered by rendering depth. Read-only; no parameters required.

**Signature:** `CameraList()`

**Returns:** `{ count, cameras: [{ name, instanceId, path, depth, orthographic, enabled }] }`

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
        var cameras = FindHelper.FindAll<Camera>();
        var list = cameras.Select(c => new
        {
            name = c.gameObject.name,
            instanceId = c.gameObject.GetInstanceID(),
            path = GameObjectFinder.GetPath(c.gameObject),
            depth = c.depth,
            orthographic = c.orthographic,
            enabled = c.enabled
        }).OrderBy(c => c.depth).ToArray();

        result.SetResult(new { count = list.Length, cameras = list });
    }
}
```
