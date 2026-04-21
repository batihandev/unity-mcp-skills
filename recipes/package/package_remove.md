# package_remove

Remove an installed package. Returns an async job when the request is accepted.

> **Native tool first:** Prefer `Unity_PackageManager_ExecuteAction` for removing packages. Use `Unity_RunCommand` with this recipe only when you need custom pre-removal checks or fine-grained job control.

**Signature:** `PackageRemove(string packageId)`

**Parameters:**
- `packageId` — Required. Installed package ID to remove.

**Returns:** `{ success, status, jobId, message, serverAvailability }` on acceptance, or `{ error }` if the package is not installed or removal fails synchronously.

**Warning:** Removal triggers Domain Reload. The REST server may be transiently unavailable. Use `job_status` or `job_wait` with the returned `jobId` to track progress.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string packageId = "com.unity.inputsystem"; // Replace with package ID to remove

        if (Validate.Required(packageId, "packageId") is object err) { result.SetResult(err); return; }

        if (!PackageManagerHelper.IsPackageInstalled(packageId))
        {
            result.SetResult(new { error = $"Package {packageId} is not installed" });
            return;
        }

        bool handledSynchronously = false;
        bool immediateSuccess = true;
        string immediateMessage = null;
        BatchJobRecord job = null;

        PackageManagerHelper.RemovePackage(packageId, (success, msg) =>
        {
            if (job == null)
            {
                handledSynchronously = true;
                immediateSuccess = success;
                immediateMessage = msg;
                return;
            }

            if (!success)
                AsyncJobService.FailJob(job.jobId, $"Package removal failed: {msg}", "failed_package");
        });

        if (handledSynchronously && !immediateSuccess)
        {
            result.SetResult(new { success = false, error = immediateMessage ?? "Package remove failed." });
            return;
        }

        job = AsyncJobService.StartPackageJob("remove", packageId);

        result.SetResult(new
        {
            success = true,
            status = "accepted",
            jobId = job.jobId,
            message = $"Removing {packageId}... Use job_status/job_wait for progress.",
            serverAvailability = ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                $"Removing package {packageId}. REST service may be briefly unavailable during package import and assembly refresh.",
                alwaysInclude: true,
                retryAfterSeconds: 8)
        });
    }
}
```
