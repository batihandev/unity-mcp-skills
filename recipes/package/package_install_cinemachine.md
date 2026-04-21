# package_install_cinemachine

Install Cinemachine using the supported package/version strategy. Handles version selection and Splines dependency automatically.

> **Native tool first:** Prefer `Unity_PackageManager_ExecuteAction` with `package_install_cinemachine` action. Use `Unity_RunCommand` with this recipe only when you need custom version control or the native tool is unavailable.

**Signature:** `PackageInstallCinemachine(int version = 3)`

**Parameters:**
- `version` — Optional. `2` for Cinemachine 2, `3` for Cinemachine 3 (default). Any value >= 3 selects CM3.

**Returns:** `{ success, status?, jobId?, message, serverAvailability? }`.

**Notes:**
- If the requested version is already installed, returns immediate `{ success: true, message }` with no job.
- CM3 auto-installs the Splines dependency.
- The version string is resolved from `PackageManagerHelper.Cinemachine3Version` / `Cinemachine2Version` at runtime.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int version = 3; // 2 for CM2, 3 for CM3

        var useV3 = version >= 3;
        var targetVersion = useV3 ? PackageManagerHelper.Cinemachine3Version : PackageManagerHelper.Cinemachine2Version;

        var status = PackageManagerHelper.GetCinemachineStatus();
        if (status.installed)
        {
            if ((useV3 && status.isVersion3) || (!useV3 && !status.isVersion3))
            {
                result.SetResult(new { success = true, message = $"Cinemachine {status.version} is already installed." });
                return;
            }
        }

        bool handledSynchronously = false;
        bool immediateSuccess = true;
        string immediateMessage = null;
        BatchJobRecord job = null;

        PackageManagerHelper.InstallCinemachine(useV3, (success, msg) =>
        {
            if (job == null)
            {
                handledSynchronously = true;
                immediateSuccess = success;
                immediateMessage = msg;
                return;
            }

            if (!success)
                AsyncJobService.FailJob(job.jobId, $"Cinemachine install failed: {msg}", "failed_package");
        });

        if (handledSynchronously && !immediateSuccess)
        {
            result.SetResult(new { success = false, error = immediateMessage ?? "Cinemachine install failed." });
            return;
        }

        job = AsyncJobService.StartPackageJob("install", PackageManagerHelper.CinemachinePackageId, targetVersion);

        var depMsg = useV3 ? " (with Splines dependency)" : "";
        result.SetResult(new
        {
            success = true,
            status = "accepted",
            jobId = job.jobId,
            message = $"Installing Cinemachine {targetVersion}{depMsg}... Use job_status/job_wait for progress.",
            serverAvailability = ServerAvailabilityHelper.CreateTransientUnavailableNotice(
                $"Installing Cinemachine {targetVersion}{depMsg}. REST service may be briefly unavailable during package import and assembly refresh.",
                alwaysInclude: true,
                retryAfterSeconds: 8)
        });
    }
}
```
