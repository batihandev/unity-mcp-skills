# editor_select

Select a GameObject in the editor and ping it in the Hierarchy.

**Signature:** `EditorSelect(string name = null, int instanceId = 0, string path = null)`

At least one of `name`, `instanceId`, or `path` must be provided.

**Returns:** `{ success, selected }` — `selected` is the resolved object name.

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
        // Provide at least one identifier
        string name = "Player";
        int instanceId = 0;
        string path = null;

        var (go, findErr) = GameObjectFinder.FindOrError(name: name, instanceId: instanceId, path: path);
        if (findErr != null) { result.SetResult(findErr); return; }

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);

        result.SetResult(new { success = true, selected = go.name });
    }
}
```
