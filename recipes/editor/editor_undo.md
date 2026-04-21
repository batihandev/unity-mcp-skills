# editor_undo

Undo the last action (single step). Flushes pending undo records before and after the operation.

For multiple steps use `history_undo(steps=N)`. For workflow-level rollback use `workflow_undo_task`.

**Signature:** `EditorUndo()`

**Returns:** `{ success, message }`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        Undo.FlushUndoRecordObjects();
        Undo.IncrementCurrentGroup();
        Undo.PerformUndo();
        Undo.FlushUndoRecordObjects();
        result.SetResult(new { success = true, message = "Undo performed" });
    }
}
```
