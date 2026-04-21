# test_get_summary

Get an aggregated test summary across all recorded runs.

**Signature:** `TestGetSummary()`

**Returns:** `{ success, totalRuns, completedRuns, totalPassed, totalFailed, totalSkipped, totalInconclusive, totalOther, allFailedTests }`

**Notes:**
- Scans the last 200 job records for real (non-synthetic) test runs
- `allFailedTests` is a deduplicated union of all failed test names across every run
- Returns `success = true` even when no runs have been recorded (counts will be zero)

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var runs = EnumerateRealTestRuns(200).ToList();
        result.SetResult(new
        {
            success = true,
            totalRuns = runs.Count,
            completedRuns = runs.Count(r => r.status == "completed"),
            totalPassed = runs.Sum(r => GetResultInt(r, "passedTests")),
            totalFailed = runs.Sum(r => GetResultInt(r, "failedTests")),
            totalSkipped = runs.Sum(r => GetResultInt(r, "skippedTests")),
            totalInconclusive = runs.Sum(r => GetResultInt(r, "inconclusiveTests")),
            totalOther = runs.Sum(r => GetResultInt(r, "otherTests")),
            allFailedTests = runs
                .SelectMany(r => GetResultStringList(r, "failedTestNames"))
                .Distinct()
                .ToArray()
        });
    }
}
```
