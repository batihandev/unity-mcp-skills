# editor_redo

Redo the last undone action (single step). Flushes pending undo records before and after the operation.

For multiple steps use `history_redo(steps=N)`.

**Signature:** `EditorRedo()`

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
        Undo.PerformRedo();
        Undo.FlushUndoRecordObjects();
        result.SetResult(new { success = true, message = "Redo performed" });
    }
}
```
