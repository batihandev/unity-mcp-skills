# test_smoke_skills

Run a broad smoke test across all registered skills, executing safe read-only ones and dry-running the rest.

**Signature:** `TestSmokeSkills(category string = null, nameContains string = null, excludeNamesCsv string = null, executeReadOnly bool = true, includeMutating bool = true, limit int = 0, runAsync bool = true, chunkSize int = 25, failureItemLimit int = 50)`

**Returns:** `{ success, status, jobId, kind, totalSkills, filters, message }` (async) or `{ success, totalSkills, executedCount, dryRunCount, skippedCount, failureCount, filters, note, results }` (sync)

**Notes:**
- Defaults to `runAsync = true`; the returned `jobId` should be polled with `job_status`/`job_wait`
- Skills with `MayTriggerReload = true` are skipped to avoid domain reload during the sweep
- `excludeNamesCsv` accepts comma- or semicolon-separated skill names
- `limit = 0` means no limit (all matching skills)

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
        string category = null;
        string nameContains = null;
        string excludeNamesCsv = null;
        bool executeReadOnly = true;
        bool includeMutating = true;
        int limit = 0;
        bool runAsync = true;
        int chunkSize = 25;
        int failureItemLimit = 50;

        var request = BuildSmokeRequest(category, nameContains, excludeNamesCsv, executeReadOnly, includeMutating, limit);

        if (runAsync)
        {
            var job = AsyncJobService.StartSmokeJob(request.SelectedSkills, request.MetadataIssues, executeReadOnly, chunkSize, failureItemLimit);
            result.SetResult(new
            {
                success = true,
                status = "accepted",
                jobId = job.jobId,
                kind = job.kind,
                totalSkills = request.SelectedSkills.Length,
                filters = new
                {
                    category,
                    nameContains,
                    excludeNames = request.ExcludedNames.OrderBy(name => name).ToArray(),
                    executeReadOnly,
                    includeMutating,
                    limit,
                    chunkSize,
                    failureItemLimit
                },
                message = "Smoke test job created. Use job_status/job_wait to monitor progress."
            });
            return;
        }

        var results = new List<object>(request.SelectedSkills.Length);
        int executedCount = 0;
        int dryRunCount = 0;
        int skippedCount = 0;
        int failureCount = 0;

        foreach (var skill in request.SelectedSkills)
        {
            var outcome = EvaluateSmokeSkill(skill, request.MetadataIssues, executeReadOnly);
            if (string.Equals(outcome.ProbeMode, "execute", StringComparison.OrdinalIgnoreCase))
                executedCount++;
            else if (string.Equals(outcome.ProbeMode, "dryRun", StringComparison.OrdinalIgnoreCase))
                dryRunCount++;

            if (string.Equals(outcome.Status, "error", StringComparison.OrdinalIgnoreCase))
                failureCount++;

            if (string.Equals(outcome.Status, "skipped", StringComparison.OrdinalIgnoreCase) ||
                (string.Equals(outcome.Status, "dryRun", StringComparison.OrdinalIgnoreCase) && !outcome.Valid.GetValueOrDefault(true)))
            {
                skippedCount++;
            }

            results.Add(new
            {
                skill = outcome.Skill,
                category = outcome.Category,
                readOnly = skill.ReadOnly,
                riskLevel = skill.RiskLevel,
                probeMode = outcome.ProbeMode,
                status = outcome.Status,
                valid = outcome.Valid,
                missingParams = outcome.MissingParams ?? Array.Empty<string>(),
                semanticWarnings = outcome.SemanticWarnings ?? Array.Empty<string>(),
                metadataWarnings = outcome.MetadataWarnings ?? Array.Empty<string>(),
                error = outcome.Error
            });
        }

        result.SetResult(new
        {
            success = failureCount == 0,
            totalSkills = request.SelectedSkills.Length,
            executedCount,
            dryRunCount,
            skippedCount,
            failureCount,
            filters = new
            {
                category,
                nameContains,
                excludeNames = request.ExcludedNames.OrderBy(name => name).ToArray(),
                executeReadOnly,
                includeMutating,
                limit
            },
            note = "Read-only skills with no required inputs are executed directly; all other skills are smoke-tested via dryRun with empty arguments.",
            results
        });
    }
}
```
