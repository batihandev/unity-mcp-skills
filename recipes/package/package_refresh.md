# package_refresh

Refresh the installed package cache used by query skills.

> **Native tool first:** Prefer `Unity_PackageManager_ExecuteAction` with action `refresh` when available. Use `Unity_RunCommand` with this recipe only when you need the returned `jobId` for explicit job tracking.

**Signature:** `PackageRefresh()`

**Parameters:** None.

**Returns:** `{ success, status, jobId, message }`.

**Note:** If a refresh is already in progress, this returns a job record for the existing refresh rather than starting a new one.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        if (PackageManagerHelper.IsRefreshing)
        {
            var existingJob = AsyncJobService.StartPackageJob("refresh", "(package_list)");
            result.SetResult(new
            {
                success = true,
                status = "accepted",
                jobId = existingJob.jobId,
                message = "Already refreshing package list..."
            });
            return;
        }

        BatchJobRecord job = null;
        PackageManagerHelper.RefreshPackageList(success =>
        {
            if (job == null)
                return;

            if (!success)
                AsyncJobService.FailJob(job.jobId, "Package list refresh failed.", "failed_package_refresh");
        });

        job = AsyncJobService.StartPackageJob("refresh", "(package_list)");
        result.SetResult(new
        {
            success = true,
            status = "accepted",
            jobId = job.jobId,
            message = "Refreshing package list..."
        });
    }
}
```
