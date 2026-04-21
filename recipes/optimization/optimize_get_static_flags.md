# optimize_get_static_flags

Read the current `StaticEditorFlags` and the `isStatic` shortcut property of a GameObject. Read-only — no changes are made.

**Signature:** `OptimizeGetStaticFlags(string name = null, int instanceId = 0, string path = null)`

**Returns:** `{ success, gameObject, flags, isStatic }`

- `flags` — string representation of the `StaticEditorFlags` enum value (e.g. `"Everything"`, `"BatchingStatic, OccluderStatic"`)
- `isStatic` — Unity's combined `GameObject.isStatic` bool shortcut

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` / `FindHelper`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // Provide at least one of: name, instanceId, or path
        string name = "Environment";
        int instanceId = 0;
        string path = null;

        var (go, error) = GameObjectFinder.FindOrError(name, instanceId, path);
        if (error != null) { result.SetResult(error); return; }

        var flags = GameObjectUtility.GetStaticEditorFlags(go);

        result.SetResult(new
        {
            success = true,
            gameObject = go.name,
            flags = flags.ToString(),
            isStatic = go.isStatic
        });
    }
}
```
