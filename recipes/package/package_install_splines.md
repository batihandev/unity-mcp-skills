# package_install_splines

Install or upgrade Unity Splines using the recommended version for the current Unity editor line.

> **Native tool first:** Prefer `Unity_PackageManager_ExecuteAction` with `package_install_splines` action. Use `Unity_RunCommand` with this recipe only when you need explicit upgrade control or the native tool is unavailable.

**Signature:** `PackageInstallSplines()`

**Parameters:** None.

**Returns:** `{ success, status?, jobId?, message, serverAvailability? }`.

**Notes:**
- If the recommended version is already installed, returns immediate `{ success: true, message }` with no job.
- The recommended version is determined at runtime by `PackageManagerHelper.GetRecommendedSplinesVersion()` (Unity 6 vs Unity 2022).
- If Splines is already installed but at a different version, the message indicates an upgrade.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var currentVersion = PackageManagerHelper.GetInstalledVersion(PackageManagerHelper.SplinesPackageId);
        var targetVersion = PackageManagerHelper.GetRecommendedSplinesVersion();

        if (currentVersion == targetVersion)
        {
            result.SetResult(new { success = true, message = $"Splines {currentVersion} is already installed." });
            return;
        }

        bool handledSynchronously = false;
        bool immediateSuccess = true;
        string immediateMessage = null;
        BatchJobRecord job = null;

        PackageManagerHelper.InstallSplines((success, msg) =>
        {
            if (job == null)
            {
                handledSynchronously = true;
                immediateSuccess = success;
                immediateMessage = msg;
                return;
            }

            if (!success)
                AsyncJobService.FailJob(job.jobId, $"Splines install failed: {msg}", "failed_package");
        });

        if (handledSynchronously && !immediateSuccess)
        {
            result.SetResult(new { success = false, error = immediateMessage ?? "Splines install failed." });
            return;
        }

        job = AsyncJobService.StartPackageJob("install", PackageManagerHelper.SplinesPackageId, targetVersion);

        result.SetResult(new
        {
            success = true,
            status = "accepted",
            jobId = job.jobId,
            message = $"Installing Splines {targetVersion}" + (currentVersion != null ? $" (upgrading from {currentVersion})" : "") + "... Use job_status/job_wait for progress.",
            serverAvailability = ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                $"Installing Splines {targetVersion}. REST service may be briefly unavailable during package import and assembly refresh.",
                alwaysInclude: true,
                retryAfterSeconds: 8)
        });
    }
}
```
