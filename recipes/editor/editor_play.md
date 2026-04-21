# editor_play

Enter play mode. Creates an async job for the mode transition.

**Signature:** `EditorPlay()`

**Returns:** `{ success, mode, jobId }` on success; `{ error }` if already in play mode.

**Risk:** medium — unsaved scene changes made during play mode are lost on exit.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if (EditorApplication.isPlaying)
        {
            result.SetResult(new { error = "Already in play mode" });
            return;
        }

        var job = AsyncJobService.CreateJob(
            "playmode", "entering_play_mode", "Entering Play Mode.", false);
        EditorApplication.isPlaying = true;
        result.SetResult(new { success = true, mode = "playing", jobId = job.jobId });
    }
}
```
