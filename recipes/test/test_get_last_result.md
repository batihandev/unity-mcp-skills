# test_get_last_result

Get the most recent test run result without needing a job ID.

**Signature:** `TestGetLastResult()`

**Returns:** `{ success, jobId, status, total, passed, failed, skipped, inconclusive, other, failedNames }`

**Notes:**
- Searches the last 100 job records for real (non-synthetic) test runs
- Returns an error object if no test runs have been recorded yet
- Field names differ slightly from `test_get_result` (e.g., `total` vs `totalTests`)

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var last = EnumerateRealTestRuns(100)
            .OrderByDescending(job => job.startedAt)
            .FirstOrDefault();

        if (last == null)
        {
            result.SetResult(new { error = "No test runs found" });
            return;
        }

        result.SetResult(new
        {
            success = true,
            jobId = last.jobId,
            status = last.status,
            total = GetResultInt(last, "totalTests"),
            passed = GetResultInt(last, "passedTests"),
            failed = GetResultInt(last, "failedTests"),
            skipped = GetResultInt(last, "skippedTests"),
            inconclusive = GetResultInt(last, "inconclusiveTests"),
            other = GetResultInt(last, "otherTests"),
            failedNames = GetResultStringList(last, "failedTestNames").ToArray()
        });
    }
}
```
