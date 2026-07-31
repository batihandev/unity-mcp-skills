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

0. **Install [`tooling/test/test_runner_tool`](../../tooling/test/test_runner_tool.md) first**, once
   per project, and start every run through it. Do **not** register a result callback from a
   `Unity_RunCommand` body: such a callback can never be cleaned up, and each one rewrites its own
   old report with every later run's results, so a report captured as "red" becomes a copy of a
   later green one. It cannot be repaired in place — the failed approaches are listed under
   [Why not inline](../../tooling/test/test_runner_tool.md#why-not-inline); do not re-attempt them.
1. Trigger: `test_run` or `test_run_by_name` calls `ProjectTestRunner.Run(...)`, which returns the
   report path immediately. `TestRunnerApi.Execute` writes no file on its own; the tool's single
   permanent callback writes it when the run finishes.
2. Use a **fresh report filename per run**, and pass **fully-qualified** test names. A bare class
   name matches zero tests, and a run of zero tests reports as a pass — always assert `total` is
   non-zero before believing a green.
3. Read: parse the report yourself once it has settled. `test_get_result`,
   `test_get_last_result` and `test_get_summary` below are **recipe templates under
   `recipes/test/`, not callable MCP tools** — adapt one into a `Unity_RunCommand` body, or just
   parse the XML from the shell, which is usually less work:
   `python3 -c "import xml.etree.ElementTree as ET;print(ET.parse('TestResults/<name>.xml').getroot().attrib)"`.

Polling across calls is the caller's job, not a recipe's. Only one Test
Runner run should be active at a time — the tool returns an `ERROR: ` string if a run it started is
still pending. Wait for a report to appear **and** for its mtime to stop changing before starting
the next.

**Precondition:** the Editor must be open and responsive on the intended
project before triggering.

**PlayMode caveat:** a PlayMode run may trigger a domain reload that discards
the in-memory callback before it fires, so no XML is written. For reliable
PlayMode reports, disable domain reload for the run or use a persistent
(compiled) editor runner. EditMode runs do not reload the domain.

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
Kick off tests. Returns `{ success, started, mode, filter, resultsPath }`
immediately; the callback writes `TestResults/<mode>-mcp.xml` when the run ends.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| testMode | string | No | EditMode | `EditMode` or `PlayMode`. |
| filter | string | No | null | Test-name substring forwarded as `Filter.testNames[0]`. |

### `test_run_by_name`
Kick off a single class or fully-qualified method. Returns
`{ success, started, testName, mode, resultsPath }`; the callback writes
`TestResults/<mode>-mcp.xml` when the run ends.

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
