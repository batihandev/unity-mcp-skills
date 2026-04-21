# test_run

Run Unity tests asynchronously; returns a job ID immediately for polling.

**Signature:** `TestRun(testMode string = "EditMode", filter string = null)`

**Returns:** `{ success, status, jobId, kind, testMode, filter, message }`

**Notes:**
- Always async — never blocks; poll with `test_get_result(jobId)` or `job_wait`
- Only one test job should be active at a time; starting a second while one is running may conflict
- `filter` is a test name substring or exact name; omit to run all tests in the specified mode

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string testMode = "EditMode";
        string filter = null;

        if (!AsyncJobService.TryStartTestJob(testMode, filter, out var job, out var error))
        {
            result.SetResult(new { success = false, error });
            return;
        }

        result.SetResult(new
        {
            success = true,
            status = "accepted",
            jobId = job.jobId,
            kind = job.kind,
            testMode,
            filter,
            message = "Tests started. Use job_status/job_wait or test_get_result(jobId) to monitor progress."
        });
    }
}
```
