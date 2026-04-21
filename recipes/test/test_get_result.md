# test_get_result

Get the result of a test run by job ID.

**Signature:** `TestGetResult(jobId string)`

**Returns:** `{ success, jobId, status, totalTests, passedTests, failedTests, skippedTests, inconclusiveTests, otherTests, failedTestNames, elapsedSeconds, resultSummary, error }`

**Notes:**
- `jobId` is required; obtain it from the `test_run` or `test_run_by_name` response
- Returns an error object if the job is not found or is not a test job
- `elapsedSeconds` counts from job start time to now; check `status` to know if the run has completed

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

        var job = AsyncJobService.Get(jobId);
        if (job == null || job.kind != "test")
        {
            result.SetResult(new { error = $"Test job not found: {jobId}" });
            return;
        }

        result.SetResult(new
        {
            success = true,
            jobId,
            status = job.status,
            totalTests = GetResultInt(job, "totalTests"),
            passedTests = GetResultInt(job, "passedTests"),
            failedTests = GetResultInt(job, "failedTests"),
            skippedTests = GetResultInt(job, "skippedTests"),
            inconclusiveTests = GetResultInt(job, "inconclusiveTests"),
            otherTests = GetResultInt(job, "otherTests"),
            failedTestNames = GetResultStringList(job, "failedTestNames").ToArray(),
            elapsedSeconds = System.Math.Max(0, System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() - job.startedAt),
            resultSummary = job.resultSummary,
            error = job.error
        });
    }
}
```
