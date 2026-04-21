# optimize_set_static_flags

Set `StaticEditorFlags` on a GameObject, optionally including all of its children. The operation is recorded in the Undo stack.

**Signature:** `OptimizeSetStaticFlags(string name = null, int instanceId = 0, string path = null, string flags = "Everything", bool includeChildren = false)`

**Returns:** `{ success, gameObject, flags, affectedCount }`

- Provide at least one of `name`, `instanceId`, or `path` to identify the target.
- Valid `flags` values: `Everything` | `Nothing` | `BatchingStatic` | `OccludeeStatic` | `OccluderStatic` | `NavigationStatic` | `ReflectionProbeStatic` (case-insensitive, comma-separated combinations allowed).
- `affectedCount` includes the root GameObject plus any children when `includeChildren = true`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Provide at least one of: name, instanceId, or path
        string name = "Environment";
        int instanceId = 0;
        string path = null;

        string flags = "Everything";        // StaticEditorFlags value(s)
        bool includeChildren = true;        // Apply recursively to all children

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        if (!System.Enum.TryParse<StaticEditorFlags>(flags, true, out var staticFlags))
        {
            result.SetResult(new { error = $"Invalid flags: {flags}" });
            return;
        }

        var targets = new List<GameObject> { go };
        if (includeChildren)
            targets.AddRange(go.GetComponentsInChildren<Transform>(true)
                               .Select(t => t.gameObject));

        foreach (var t in targets)
        {
            Undo.RecordObject(t, "Set Static Flags");
            GameObjectUtility.SetStaticEditorFlags(t, staticFlags);
        }

        result.SetResult(new
        {
            success = true,
            gameObject = go.name,
            flags = staticFlags.ToString(),
            affectedCount = targets.Count
        });
    }
}
```
