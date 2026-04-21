# test_cancel

Cancel a running test job if supported.

**Signature:** `TestCancel(jobId string = null)`

**Returns:** `{ success, jobId, status, cancelled, note, warnings }`

**Notes:**
- Unity TestRunnerApi does not support hard cancellation; the job layer reports the best available state
- `jobId` is validated as required even though the parameter has a default of `null`
- `cancelled` is `true` only when `job.status == "cancelled"`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string jobId = "YOUR_JOB_ID";

        if (Validate.Required(jobId, "jobId") is object err)
        {
            result.SetResult(err);
            return;
        }

        var job = AsyncJobService.Cancel(jobId);
        if (job == null || job.kind != "test")
        {
            result.SetResult(new { error = $"Test job not found: {jobId}" });
            return;
        }

        result.SetResult(new
        {
            success = true,
            jobId = job.jobId,
            status = job.status,
            cancelled = job.status == "cancelled",
            note = "Unity TestRunnerApi does not support direct cancellation. The unified job layer only reports supported cancellation states.",
            warnings = job.warnings
        });
    }
}
```
