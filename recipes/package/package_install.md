# package_install

Install a Unity package by ID. Returns an async job when the request is accepted.

> **Native tool first:** Prefer `Unity_PackageManager_ExecuteAction` for installing packages. Use `Unity_RunCommand` with this recipe only when you need fine-grained job control or custom error handling not available through the native tool.

**Signature:** `PackageInstall(string packageId, string version = null)`

**Parameters:**
- `packageId` — Required. Package ID to install (e.g. `com.unity.inputsystem`).
- `version` — Optional. Explicit version string; omit to install the recommended version.

**Returns:** `{ success, status, jobId, message, serverAvailability }` on acceptance, or `{ success: false, error }` on synchronous failure.

**Warning:** Install triggers package import and Domain Reload. The REST server may be transiently unavailable. Use `job_status` or `job_wait` with the returned `jobId` to track progress.

## Prerequisites

Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
- `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
- `recipes/_shared/validate.md` — for `Validate.Required` / `Validate.SafePath`

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        string packageId = "com.unity.inputsystem"; // Replace with target package ID
        string version = null; // Optional: e.g. "1.7.0", or leave null for recommended

        if (Validate.Required(packageId, "packageId") is object err) { result.SetResult(err); return; }

        bool handledSynchronously = false;
        bool immediateSuccess = true;
        string immediateMessage = null;
        BatchJobRecord job = null;

        PackageManagerHelper.InstallPackage(packageId, version, (success, msg) =>
        {
            if (job == null)
            {
                handledSynchronously = true;
                immediateSuccess = success;
                immediateMessage = msg;
                return;
            }

            if (!success)
                AsyncJobService.FailJob(job.jobId, $"Package install failed: {msg}", "failed_package");
        });

        if (handledSynchronously && !immediateSuccess)
        {
            result.SetResult(new { success = false, error = immediateMessage ?? "Package install failed." });
            return;
        }

        job = AsyncJobService.StartPackageJob("install", packageId, version);

        result.SetResult(new
        {
            success = true,
            status = "accepted",
            jobId = job.jobId,
            message = $"Installing {packageId}" + (version != null ? $"@{version}" : "") + "... Use job_status/job_wait for progress.",
            serverAvailability = ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                $"Installing package {packageId}. REST service may be briefly unavailable during package import and assembly refresh.",
                alwaysInclude: true,
                retryAfterSeconds: 8)
        });
    }
}
```
