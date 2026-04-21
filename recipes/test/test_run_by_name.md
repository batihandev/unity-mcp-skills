# test_run_by_name

Run specific Unity tests by class or method name, returning a job ID for polling.

**Signature:** `TestRunByName(testName string, testMode string = "EditMode")`

**Returns:** `{ success, status, jobId, testName, testMode }`

**Notes:**
- `testName` is required; pass the exact test class name or fully qualified method name
- Uses the same async job model as `test_run`; poll with `test_get_result(jobId)`
- Only one active test job at a time is recommended

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string testName = "MyTestClass";
        string testMode = "EditMode";

        if (Validate.Required(testName, "testName") is object err)
        {
            result.SetResult(err);
            return;
        }

        if (!AsyncJobService.TryStartTestJob(testMode, testName, out var job, out var error))
        {
            result.SetResult(new { success = false, error });
            return;
        }

        result.SetResult(new
        {
            success = true,
            status = "accepted",
            jobId = job.jobId,
            testName,
            testMode
        });
    }
}
```
