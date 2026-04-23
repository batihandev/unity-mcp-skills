# project_get_tags

Get all tag definitions from the project's TagManager. Read-only; no parameters required.

**Signature:** `ProjectGetTags()`

**Returns:** `{ success, count, tags: string[] }`

## Notes

- Tags are read-only via this command. To add a tag programmatically use `project_add_tag`.
- Built-in Unity tags (`Untagged`, `Respawn`, `Finish`, `EditorOnly`, `MainCamera`, `Player`, `GameController`) are included in the result.

**Prerequisites:** [`execution_result`](../_shared/execution_result.md)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var tags = UnityEditorInternal.InternalEditorUtility.tags;
        result.SetResult(new { success = true, count = tags.Length, tags });
    }
}
```
