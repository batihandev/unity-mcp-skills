---
name: unity-test
description: "Use when users want to run Unity tests or read test results."
---

# Test Skills

## Overview

Kick off Unity tests and read their XML reports. Recipes are stateless
fire-and-forget + read pairs — there is no job ID, no polling loop inside a
single `Unity_RunCommand`. Trigger a run in one call, read `TestResults/*.xml`
in a later call.

## Mental model

1. Trigger: `test_run` or `test_run_by_name` calls `TestRunnerApi.Execute(...)`
   and returns `{ started: true }`.
2. Unity runs the tests off-thread and writes an NUnit-format XML file to
   `<project-root>/TestResults/EditMode-*.xml` or `PlayMode-*.xml`.
3. Read: `test_get_result`, `test_get_last_result`, or `test_get_summary`
   parse the XML and return counts + failed names.

Polling across calls is the caller's job, not a recipe's. Only one Test
Runner run should be active at a time.

## Common Mistakes

**DO NOT** (common hallucinations):
- `test_run_all` does not exist → use `test_run` or `test_run_by_name`.
- `test_create_template` does not exist → use `test_create_editmode` or
  `test_create_playmode`.
- `test_get_status` does not exist → use `test_get_result` (reads the XML,
  stateless).
- There is no `jobId` anywhere. If older docs mention one, ignore them.
- There is no `test_cancel` — Unity `TestRunnerApi` has no public hard-cancel
  surface.
- There is no `test_smoke_skills` — it depended on an upstream REST skill
  registry that isn't in this pack.

**Routing**:
- For compile error checking → `editor_get_state` (`isCompiling` field).
- For test script creation → `test_create_editmode` / `test_create_playmode`,
  then edit via the `script` module.

## Skills

### `test_run`
Kick off tests. Returns `{ success, started, mode, filter }` immediately.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| testMode | string | No | EditMode | `EditMode` or `PlayMode`. |
| filter | string | No | null | Test-name substring forwarded as `Filter.testNames[0]`. |

### `test_run_by_name`
Kick off a single class or fully-qualified method. Returns
`{ success, started, testName, mode }`.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| testName | string | Yes | - | Exact class name or `Ns.Class.Method`. |
| testMode | string | No | EditMode | `EditMode` or `PlayMode`. |

### `test_get_result`
Read the newest `TestResults/<mode>-*.xml` and return parsed counts.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| testMode | string | No | EditMode | Which XML family to filter on. |

**Returns:** `{ success, file, total, passed, failed, skipped, inconclusive, failedNames, startTime, endTime, durationSeconds }`

### `test_get_last_result`
Newest XML across all modes. No parameters.

**Returns:** `{ success, file, mode, total, passed, failed, skipped, inconclusive, failedNames, startTime, endTime, durationSeconds }`

### `test_get_summary`
Aggregate every XML report under `TestResults/`. No parameters.

**Returns:** `{ success, totalRuns, totalPassed, totalFailed, totalSkipped, totalInconclusive, allFailedTests, files }`

### `test_list`
List available tests.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| testMode | string | No | EditMode | `EditMode` or `PlayMode`. |
| limit | int | No | 100 | Max tests to list. |

### `test_list_categories`
List test categories.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| testMode | string | No | EditMode | `EditMode` or `PlayMode`. |

### `test_create_editmode`
Write an EditMode test template synchronously.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| testName | string | Yes | - | Class name. No `/`, `\`, `..`. |
| folder | string | No | Assets/Tests/Editor | Must start with `Assets/` or `Packages/`. |

**Returns:** `{ success, path, testName }`

### `test_create_playmode`
Write a PlayMode test template synchronously.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| testName | string | Yes | - | Class name. No `/`, `\`, `..`. |
| folder | string | No | Assets/Tests/Runtime | Must start with `Assets/` or `Packages/`. |

**Returns:** `{ success, path, testName }`

---
## RunCommand Examples

Recipe path rule: `../../recipes/test/<command>.md`

*See `../../recipes/test/<command>.md` for C# templates.*
