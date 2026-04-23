# editor_get_tags

Get all tags defined in the project's tag manager.

**Signature:** `EditorGetTags()`

**Returns:** `{ tags: [string] }`

Note: no top-level `success` key. `tags` is a string array from `InternalEditorUtility.tags`.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

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
