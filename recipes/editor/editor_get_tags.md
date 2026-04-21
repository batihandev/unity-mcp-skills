# editor_get_tags

Get all tags defined in the project's tag manager.

**Signature:** `EditorGetTags()`

**Returns:** `{ tags: [string] }`

Note: no top-level `success` key. `tags` is a string array from `InternalEditorUtility.tags`.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        result.SetResult(new { tags = InternalEditorUtility.tags });
    }
}
```
