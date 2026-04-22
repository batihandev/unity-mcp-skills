# Recipes Compile-Readiness Repair Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
> **Status:** Active plan; session 2 scope appended 2026-04-21 evening. Session 1 delivered ext 484/484, pre 484/484, comp 19/484. Session 2 scope is Tasks 11–21 below; revised execution order at the end of the doc supersedes the Task 0–10 wave for remaining work.

**Goal:** Make every non-async recipe in `recipes/**` compile and execute successfully inside the current official Unity MCP `Unity_RunCommand` environment, document the small set of genuinely async recipes honestly, and fix the broken recipe-discovery path in every `SKILL.md` so AI agents can find the right template instead of inventing their own.

**Architecture:** This repo is documentation-only (per root `SKILL.md` and `README.md` — `MIGRATION.md` and `agent.md` were removed in commit `dfde255` on 2026-04-21; the root `SKILL.md` is now the single entry point and source of truth for framing). No C# asmdef will be shipped. Helpers that the recipes depend on (`Validate`, `GameObjectFinder`, `FindHelper`, `WorkflowManager`, `ExecutionResult.SetResult`) must therefore exist as **paste-in templates** under `recipes/_shared/<helper>.md` and be pulled into the `Unity_RunCommand` call by the agent when the recipe declares the dependency. This preserves the repo's "zero install, pure docs" promise while making the recipes actually runnable.

**Tech Stack:** Markdown, ripgrep, shell, Unity Editor `com.unity.ai.assistant` MCP package, `Unity_RunCommand` C# execution environment, `Newtonsoft.Json` (already present in Unity via `com.unity.nuget.newtonsoft-json`).

**Execution Rules For This Plan:**
- Use `superpowers:subagent-driven-development` task-by-task.
- After every accepted task, append a new entry to the workflow notes file created in Task 0.
- No commits unless the user explicitly asks.
- When a task requires Unity to verify a recipe actually compiles, use `Unity_RunCommand` with the MCP tools — do **not** assume a recipe is fine just because it looks plausible. Stale extractor output has already caused silent breakage once.
- If a task reveals that a dependency or pattern is more widespread than the audit found, stop and update the plan before continuing.

---

## Why This Plan Exists

Commit `dfde255` (2026-04-21) removed `MIGRATION.md` and `agent.md` and collapsed framing into the root `SKILL.md` + `README.md`. That cleanup did **not** touch recipes or `_shared/` helpers; the compile-readiness problems below are still present in the tree as of the latest commit (`b68086a`). A dependency audit against the live Unity AI Assistant MCP package (`com.unity.ai.assistant@fc16ca46e208`) on 2026-04-21 against `HEAD` shows:

### Hard breakage (does not compile in current `Unity_RunCommand`)

| Dependency | Files using it (HEAD) | What exists today | Status |
|---|---|---|---|
| `result.SetResult(...)` | 299 / 522 recipes | No shared helper; not on current `ExecutionResult` | **broken** |
| `WorkflowManager.SnapshotObject(...)` / other `WorkflowManager.*` | 174 recipes | No shared helper | **broken** |
| `GameObjectFinder.*` | 154 recipes | Template exists at `recipes/_shared/gameobject_finder.md` but no recipe references it | **works if pasted, but recipes do not declare the dep** |
| `Validate.Required` / `Validate.SafePath` | 113 recipes | `recipes/_shared/validate.md` exists but is a broken stub (contains literal `bool true = default;` garbage) | **broken** |
| `FindHelper.FindAll<T>(...)` | 20 recipes | Template exists at `recipes/_shared/gameobject_finder.md` (bundled with `GameObjectFinder`) | **works if pasted, but recipes do not declare the dep** |
| `AsyncJobService.TryStartTestJob(...)` / `AsyncJobService.Get(...)` / `.jobId` / `.startedAt` | 13 recipes | No shared helper possible (requires cross-call state persistence) | **fundamentally incompatible with stateless `Unity_RunCommand`** |

### Soft breakage (compiles but AI cannot find it)

- Both `skills/SKILL.md` and `recipes/README.md` contain the line `Recipe path rule: ../../recipes/<topic>/<command>.md`. That relative path resolves from the skill's **filesystem** location to the recipe's filesystem location. Most AI agents load skills through a platform-curated surface (e.g. Claude Code's `~/.claude/skills/unity-mcp-skills/...`). Even when the install is one folder, the `../../recipes/...` form is brittle across symlinks, worktrees, and platforms. The result: agents sometimes never read the recipe and fall back to guessing. This is how the `test` skill session on 2026-04-21 ended up using `ExecuteMenuItem` + XML polling instead of the intended async pattern.
- The root `SKILL.md` now (post-`dfde255`) correctly lists domain skill paths and has an integrity-check block, but the per-skill `SKILL.md` files still describe `test_run`, `test_get_result`, `gameobject_create`, etc. as if they are first-class MCP tool names. They are not — they are recipe filenames. Without a clear "these are templates to paste into `Unity_RunCommand`" framing **per-skill**, agents hallucinate non-existent MCP tools.

### Additional hard breakage (discovered mid-implementation on 2026-04-21)

`rg -l "Original Logic:" recipes/` returns **82 recipes**. In each one the real
C# logic is trapped inside a `/* Original Logic: */ ... */` comment block, and the
method body above contains garbage placeholders (e.g. `bool true = default;`,
`string "Assets" = default;`) that do not compile. Examples confirmed:
`recipes/gameobject/gameobject_create.md`, `recipes/component/component_add.md`,
`recipes/_shared/validate.md` (pre-Task-1). These recipes did not fail the
"uses `IRunCommand`" audit because the `IRunCommand` boilerplate is intact —
only the body is stubbed. They need Task 5b below to promote the comment-block
body into real executable code.

### Scope signal

`IRunCommand` / `ExecutionResult` appear in ~487 of 522 recipes — the core execution contract is fine. The breakage is in **six missing or broken helpers**, **~82 half-extracted recipe bodies**, and **one broken skill-to-recipe pointer**, not in recipe domain logic (the logic survives, it's just commented out).

---

## Non-Goals

- **Do not** introduce a C# asmdef, plugin, or runtime DLL into this repo. The repo's stated philosophy (root `SKILL.md` + `README.md`) is documentation-only. Any fix must stay pure markdown + paste-in templates.
- **Do not** try to make the 13 async recipes pretend to be synchronous inside `Unity_RunCommand`. Cross-call state persistence is not available in stateless RunCommand; those 13 need honest redesign.
- **Do not** mass-rewrite all 299 `SetResult` call sites into `result.Log(JsonConvert.SerializeObject(...))`. A single shared extension method preserves the existing call pattern and is far safer than 299 mechanical edits.
- Performance tuning of helpers beyond what's needed for compile + basic correctness.
- Re-verifying recipes that the audit did not flag (the Unity domain logic inside each recipe is assumed correct unless smoke-test in Task 7 proves otherwise).

---

## Architectural Decisions (locked before task breakdown)

1. **Helpers live in `recipes/_shared/*.md` as paste-in templates.** Never as a ported asmdef, never as a plugin. Each helper file contains a self-contained `internal static class` (or extension class) that the agent pastes alongside `CommandScript` inside the `Unity_RunCommand` code block.
2. **Recipes declare dependencies via a `## Prerequisites` section.** A recipe that uses `GameObjectFinder` declares `## Prerequisites` listing `recipes/_shared/gameobject_finder.md`, so the agent knows to read that file and concatenate its helper class into the same `Unity_RunCommand` call. Without this, the existing `recipes/_shared/gameobject_finder.md` template is invisible to consumers.
3. **`SetResult` is preserved as an extension method, not rewritten across 299 files.** A new `recipes/_shared/execution_result.md` defines `public static void SetResult(this ExecutionResult r, object payload)` as a thin wrapper over `r.Log(Newtonsoft.Json.JsonConvert.SerializeObject(payload))`. All 299 existing call sites keep working once they declare the prerequisite.
4. **`WorkflowManager` is replaced with a thin paste-in that maps to standard Unity APIs.** The original `WorkflowManager.SnapshotObject(go)` call mapped to `Undo.RegisterCompleteObjectUndo(go, "...")` plus optional tracking. The shared template wraps the standard Unity undo API so the 174 call sites stay intact. Non-essential tracking (session history, named workflows) is dropped — it was a REST-era feature with no MCP analog.
5. **The 13 async recipes get redesigned, not patched.** Each is split into a `<name>_start.md` (fire-and-forget) and a `<name>_read.md` (stateless read from Unity's own persisted surface: `TestResults/*.xml` for tests, package manifest for packages, etc.). The old `<name>.md` file is replaced with a dispatcher recipe that the AI reads once to learn the pattern.
6. **Skill → recipe discovery uses explicit content, not relative paths.** Every per-domain `SKILL.md` ends with a `## RunCommand Templates` section that inlines the full recipe filenames and explains the load pattern. The root `SKILL.md` (post-`dfde255`) already uses a repo-rooted `skills/<domain>/SKILL.md` convention — per-domain skills must adopt the same repo-rooted form (`recipes/<domain>/<command>.md`) and keep the `../../recipes/...` variant only as a secondary convenience pointer, never the primary.

---

## Task 0: Baseline evidence, upstream pin, and workflow notes setup

**Intent:** Freeze the current state as evidence, clone the upstream source at a pinned SHA so later tasks can re-extract recipe bodies and verify helper signatures, create the notes file this plan's future tasks will append to, and confirm the audit numbers before touching anything.

**Upstream pin:**
- Repo: `https://github.com/Besty0728/Unity-Skills`
- SHA: `55b03ef32de920f4f2d884c9eed1491a535c2ae5`
- Clone command: `git clone https://github.com/Besty0728/Unity-Skills.git /tmp/upstream-unity-skills && cd /tmp/upstream-unity-skills && git checkout 55b03ef3`
- Source-of-truth mapping: recipe filename `<topic>_<command>.md` → upstream method `UnitySkills.<Topic>Skills.<Command>` in `SkillsForUnity/Editor/Skills/<Topic>Skills.cs`. Helpers (`Validate`, `FindHelper`, `GameObjectFinder`, `SkillsCommon`) all live in `SkillsForUnity/Editor/Skills/GameObjectFinder.cs`. `ComponentSkills.FindComponentType` + `ConvertValue` live in `SkillsForUnity/Editor/Skills/ComponentSkills.cs`. `AsyncJobService` lives in `SkillsForUnity/Editor/Skills/AsyncJobService.cs` and is deliberately NOT ported (requires `[InitializeOnLoad]` + file-backed `BatchPersistence`).

**Files:**
- Create: `docs/superpowers/notes/2026-04-21-recipes-compile-readiness-notes.md`

**Required outcomes:**
- Notes file exists with the task-note template (four buckets: friction, stale-doc mismatches, redundant-systems, reusable skill/tool candidates).
- A fresh dependency-count snapshot is captured in the notes as the baseline. Expected starting counts at HEAD `b68086a`:
  - `result.SetResult` — 299 files
  - `WorkflowManager.` — 174 files
  - `GameObjectFinder.` — 154 files
  - `Validate.` — 113 files
  - `FindHelper.` — 20 files
  - `AsyncJobService` — 13 files
  - total recipes — 522; `IRunCommand` references — 487
  - If the audit diverges by more than ±5 from these, stop and update the plan.
  - Enumerate the 13 async recipe filenames with `rg -l AsyncJobService recipes/` — do not assume the historical list.
- No recipe or skill is modified in Task 0.

**Verification target:**
- Re-running the audit commands in Task 0 produces the same counts referenced in this plan.

---

## Task 1: Fix `recipes/_shared/validate.md` from a broken stub to a working helper

**Intent:** Replace the current broken `_shared/validate.md` (which contains illegal placeholders like `bool true = default;` inside TODO blocks and the real logic in `/* Original Logic: */` comments) with a clean paste-in helper class that actually compiles.

**Files:**
- Rewrite: `recipes/_shared/validate.md`

**Required outcomes:**
- `recipes/_shared/validate.md` contains a single self-contained `internal static class Validate` block with at least these members used by existing recipes: `Required(string value, string name)`, `Required(object value, string name)`, `SafePath(string path, string name)`. Audit current usage in `recipes/**` to confirm the final method list — do not add methods that no recipe calls.
- Each method returns `null` on success and an error object (`new { error = "..." }`) on failure, matching the `is object err` pattern already used at every call site.
- Usage comment at the top shows the standard paste pattern: paste `Validate` class inside the same code block as `CommandScript`, then `if (Validate.Required(x, "x") is object err) { result.SetResult(err); return; }`.
- No placeholder `bool true = default;` lines or `/* Original Logic: */` comments remain.

**Verification target:**
- One real recipe that currently uses `Validate.Required` (e.g. `recipes/physics/physics_raycast.md`) compiles successfully inside a real `Unity_RunCommand` call after concatenating the new `_shared/validate.md` helper class into the same code block. Use the MCP to run the compile check — do not assume.

---

## Task 2: Create `recipes/_shared/execution_result.md` for the `SetResult` extension

**Intent:** Make `result.SetResult(new { ... })` work everywhere it is currently called without touching any of the 299 call sites.

**Files:**
- Create: `recipes/_shared/execution_result.md`

**Required outcomes:**
- New shared file defines a single `internal static class ExecutionResultExtensions` with `public static void SetResult(this ExecutionResult r, object payload)`.
- The implementation uses `Newtonsoft.Json.JsonConvert.SerializeObject(payload)` and forwards to `r.Log(...)` so the structured return shows up in `executionLogs`. Do **not** use `UnityEngine.JsonUtility.ToJson` — it does not serialize anonymous types, which ~all SetResult call sites use.
- The file documents the required `using Newtonsoft.Json;` (or fully-qualified call) and notes the package `com.unity.nuget.newtonsoft-json` requirement.
- The file states that the extension is a paste-in (not a ported asmdef) and must be included alongside `CommandScript` in every recipe that calls `result.SetResult(...)`.

**Verification target:**
- One recipe that calls `result.SetResult(new { success = true, foo = 1 })` — for example `recipes/scene/scene_get_info.md` — compiles and returns the JSON payload in `executionLogs` when the new extension class is concatenated into the `Unity_RunCommand` code block.

---

## Task 3: Create `recipes/_shared/workflow_manager.md` as an undo/tracking shim

**Intent:** Replace the 174 call sites of `WorkflowManager.SnapshotObject(go)` and related `WorkflowManager.*` calls with a paste-in shim that maps to standard Unity editor APIs.

**Files:**
- Create: `recipes/_shared/workflow_manager.md`
- Reference: run `rg "WorkflowManager\." recipes/` to enumerate every call pattern that needs coverage before writing the shim.

**Required outcomes:**
- Full enumeration of `WorkflowManager.*` call patterns across all 174 recipes. Do not guess; grep them all.
- `recipes/_shared/workflow_manager.md` defines `internal static class WorkflowManager` with a method signature that matches every enumerated call pattern.
- Behavior minimal but honest: `SnapshotObject(UnityEngine.Object obj)` calls `Undo.RegisterCompleteObjectUndo(obj, "Workflow Snapshot")`. Session/history tracking APIs — if enumerated — either (a) become no-ops with a comment explaining why, or (b) are replaced by the standard Unity equivalent if one cleanly exists.
- Shim documents that non-essential tracking state was intentionally dropped as part of the REST-era cleanup.

**Verification target:**
- A recipe that calls `WorkflowManager.SnapshotObject(go)` (e.g. `recipes/gameobject/gameobject_set_transform.md`, verify via `rg`) compiles and correctly registers an undo entry when the shim is concatenated.

---

## Task 3b: Split `ComponentSkills` surface into two paste-in helpers

**Intent:** Six recipes call `ComponentSkills.FindComponentType` or `ComponentSkills.ConvertValue`. Mid-implementation audit (2026-04-21) revealed this — the earlier plan missed it because the grep scope was `Validate|GameObjectFinder|FindHelper|WorkflowManager`, not `ComponentSkills`. Port the two needed surfaces as **two separate** paste-in files so recipes that only need one can keep their concatenated payload small.

**Files:**
- Create: `recipes/_shared/component_type_finder.md` — exposes `internal static class ComponentSkills` with only `FindComponentType(string)` + private `TryGetTypeFromAssemblies` + private `_typeCache` + `ExtendedNamespaces[]`. Depends on `SkillsCommon.GetAllLoadedTypes`, so recipes using this file must also declare `_shared/skills_common.md` as a prerequisite.
- Create: `recipes/_shared/value_converter.md` — exposes `internal static class ComponentSkills` with only `ConvertValue(string, Type)` + all private `Parse*` helpers (`ParseBool`, `ParseVector2/3/4`, `ParseVector2Int/3Int`, `ParseQuaternion`, `ParseColor`, `ParseColor32`, `GetNamedColor`, `ParseRect`, `ParseBounds`, `ParseLayerMask`, `ParseAnimationCurve`, `ParseFloatArray`, `ParseIntArray`).
- Note: both files expose `internal static class ComponentSkills`, but a recipe never needs both in the same RunCommand block — `FindComponentType` callers don't parse values, and `ConvertValue` callers don't search for types. If a future recipe needs both, merge the two files into a single `_shared/component_skills.md` on demand.

**Required outcomes:**
- Verbatim extract from upstream `ComponentSkills.cs` at SHA `55b03ef3`, stripped of unrelated methods (~900 of 1028 lines are drop). Match upstream signatures exactly (same `public` vs `internal`, same param names).
- `component_type_finder.md` is ~90 lines of pasteable C#; `value_converter.md` is ~180 lines.
- Each file documents its paste pattern and (for `component_type_finder.md`) the transitive dependency on `skills_common.md`.

**Verification target:**
- One recipe that calls `FindComponentType` (e.g. `recipes/prefab/prefab_set_property.md`) compiles with `_shared/component_type_finder.md` + `_shared/skills_common.md` concatenated.
- One recipe that calls `ConvertValue` (e.g. `recipes/scriptableobject/scriptableobject_set.md`) compiles with `_shared/value_converter.md` concatenated.

---

## Task 4: Confirm `recipes/_shared/gameobject_finder.md` covers all current call patterns

**Intent:** The existing `_shared/gameobject_finder.md` already defines `FindHelper` and `GameObjectFinder`, but there was never a cross-check that it actually covers every symbol used by the 156 + 22 consuming recipes.

**Files:**
- Audit and possibly modify: `recipes/_shared/gameobject_finder.md`

**Required outcomes:**
- Run `rg "GameObjectFinder\.\w+|FindHelper\.\w+" recipes/` and produce a sorted unique list of member calls.
- For each member call, confirm it exists in `_shared/gameobject_finder.md`. If any member is missing (e.g. `GameObjectFinder.GetPath` appears in dozens of recipes — verify it is defined), add the missing member to the shared template using the original upstream implementation as reference if needed.
- Confirm the shared file is self-contained (no hidden references to other removed helpers).

**Verification target:**
- A recipe that exercises `GameObjectFinder.GetPath` (e.g. `recipes/validation/validate_scene.md`) compiles cleanly with the shared helper concatenated.

---

## Task 5: Redesign the 13 async recipes honestly

**Intent:** The stateless `Unity_RunCommand` environment cannot preserve a `jobId` between calls. Emulating that via a paste-in helper would only hide the failure mode. Redesign each of the 13 into a fire-and-forget + stateless-read pair, or document the genuine limitation.

**Files:**
- Rewrite: each of the 13 recipes currently using `AsyncJobService` (verified list from Task 0).
- Modify: `skills/test/SKILL.md`, `skills/package/SKILL.md`, `skills/editor/SKILL.md` to reflect the new recipe names and pattern.

**Required outcomes:**
- For tests:
  - `test_run.md` → trigger `EditorApplication.ExecuteMenuItem("Tools/Tests/Run All EditMode")` or programmatic `TestRunnerApi` kickoff (no polling). Return immediately with `{ success, started, mode, filter }`.
  - `test_get_result.md` → read `TestResults/EditMode-all-menu.xml` (or `PlayMode-all-menu.xml`) via `System.Xml` and return parsed `{ total, passed, failed, failedNames, startTime, endTime }`. No `jobId` parameter.
  - `test_run_by_name.md` → trigger via `TestRunnerApi` with a filter; read via same `test_get_result` pattern.
  - `test_cancel.md` → either remove (native MCP or user action) or implement as "cancel the most recent run via `TestRunnerApi`" and document the limitation.
  - `test_get_last_result.md` → alias of `test_get_result.md` with "the newest XML in `TestResults/`" semantics.
  - `test_get_summary.md` → either remove (no aggregate storage exists) or implement by globbing all `TestResults/*.xml`.
  - `test_smoke_skills.md` → evaluate whether this recipe is even salvageable without the REST skill registry; if not, remove it and note why.
  - `test_create_editmode.md` / `test_create_playmode.md` → these don't actually need async; they just create a `.cs` file. Rewrite as synchronous templates using `File.WriteAllText` + `AssetDatabase.ImportAsset`.
- For packages:
  - `package_install.md` → use `UnityEditor.PackageManager.Client.Add(...)` and return the `Request` state at the moment of the call. Document that full completion requires a second `package_list.md` read a few seconds later.
  - `package_install_cinemachine.md` / `package_install_splines.md` → same pattern as above.
  - `package_refresh.md` → call `Client.Resolve()` and return immediately.
  - `package_remove.md` → call `Client.Remove(...)` with the same pattern as install.
- For editor:
  - `editor_play.md` → call `EditorApplication.EnterPlaymode()` and return immediately. No polling; a separate `editor_get_state.md` read is the way to observe state.
- Each rewritten recipe documents the stateless constraint in a `## Pattern` section so future readers understand why there is no `jobId`.
- `skills/test/SKILL.md`, `skills/package/SKILL.md`, `skills/editor/SKILL.md` each updated to describe the two-step fire-and-forget + read pattern where it applies, and to remove the stale "returns a `jobId`, poll with `test_get_result(jobId)`" guidance.

**Verification target:**
- `test_run.md` followed by `test_get_result.md` successfully returns pass/fail counts for this repo's own dogfood test project (or a named Unity project provided by the human operator).
- `package_install.md` successfully installs a small harmless package (e.g. `com.unity.ide.visualstudio`) and `package_list.md` afterwards shows it present.

---

## Task 5b: De-stub the 82 recipes whose body is trapped in `/* Original Logic: */`

**Intent:** Restore the real C# logic for every recipe that was half-extracted. The outer method body currently contains garbage placeholders (`bool true = default;`, `string "Assets" = default;`, `int 50 = default;`) above a `/* Original Logic: */` comment block that holds the real logic. The recipe compiles (barely — some of those placeholders are themselves illegal identifiers) but executes a no-op.

**Files:**
- Modify: the 82 files returned by `rg -l "Original Logic:" recipes/`. Enumerate at Task start — do not rely on this number.

**Required outcomes:**
- For each affected recipe:
  1. Remove the lines that look like `<type> <literal> = default; // Assign value` (the "TODO: Replace parameters" block). These were placeholder garbage emitted by a broken extractor.
  2. Keep any legitimate parameter-default declarations at the top of `Execute` that use real identifier names (`string name = "MyObject";`, etc.) — these are intentional example values. Distinguish via: left-hand side must be a valid C# identifier, not a literal.
  3. Unwrap the `/* Original Logic: ... */` block. Its contents become the new method body.
  4. Translate `return <payload>;` inside the unwrapped block to `result.SetResult(<payload>); return;` (since `IRunCommand.Execute` returns void, not `object`). Preserve the semantics: early-return error objects become `result.SetResult(err); return;`.
  5. If the unwrapped block uses any of `Validate.*`, `GameObjectFinder.*`, `FindHelper.*`, `WorkflowManager.*`, `SkillsCommon.*`, or `result.SetResult`, make sure Task 6's prerequisite-detector picks it up after the de-stub.
- Do not introduce new logic. This is a pure restoration: uncomment, re-shape return statements, delete literal-named placeholders.
- Non-essential `// TODO: Replace parameters with your actual logic` banner comment is removed.

**Verification target:**
- `rg -l "Original Logic:" recipes/` returns zero matches after the task completes.
- `rg -N '[a-z]+ ("[^"]+"|[0-9]+|true|false) = default;' recipes/` returns zero matches (catches leftover literal-name placeholders).
- Spot-compile at least one recipe per affected category via `Unity_RunCommand` to confirm the de-stubbed body runs; record results in the notes file.

**Execution plan:**
- Write a small de-stub script (Python or `sed`+`awk`) that handles the common shape. Hand-fix any recipe the script cannot safely transform.
- Run Task 5b **before** Task 6, because the prerequisite-detector in Task 6 grep the method body — a stubbed recipe has no helper calls in its (empty) body, so it would be missed.

---

## Task 6: Add `## Prerequisites` declarations to every recipe that uses a shared helper

**Intent:** Right now a recipe that uses `GameObjectFinder` gives no indication that the agent needs to also paste the `_shared/gameobject_finder.md` class. Declaring dependencies explicitly is how the agent knows which files to concatenate into a single `Unity_RunCommand` call.

**Files:**
- Modify: every recipe that calls `Validate.*`, `GameObjectFinder.*`, `FindHelper.*`, `WorkflowManager.*`, or `result.SetResult`.

**Required outcomes:**
- Each affected recipe gains a `## Prerequisites` section listing the `_shared/*.md` files it depends on. Example:
  ```markdown
  ## Prerequisites

  Concatenate these shared helper classes into the same `Unity_RunCommand` code block as `CommandScript`:
  - `recipes/_shared/execution_result.md` — for `result.SetResult(...)`
  - `recipes/_shared/gameobject_finder.md` — for `GameObjectFinder` and `FindHelper`
  ```
- The `## Prerequisites` section sits immediately before the ```csharp fenced code block so the agent reads it first.
- A recipe that needs **no** shared helper gets no Prerequisites section (do not add empty sections as filler).
- The detection is scripted (`rg` the symbol usage per recipe) — not manual. Manual spot-checks verify a 10% sample.

**Verification target:**
- A sampled recipe from each of: scene, ui, scriptableobject, terrain, validation, gameobject — declares its helper dependencies correctly. No sampled recipe is missing a dependency it actually uses.

---

## Task 7: Compile-smoke one recipe per category against live Unity MCP

**Intent:** Prove that after Tasks 1–6 a recipe + its declared Prerequisites actually compiles and runs. Prevent another "migration complete but nothing works" episode.

**Files:**
- Create: `docs/superpowers/notes/2026-04-21-recipes-compile-readiness-notes.md` (smoke-test results appended)

**Required outcomes:**
- For each top-level recipe category (animator, asset, camera, cinemachine, cleaner, component, console, editor, event, gameobject, importer, light, material, navmesh, optimization, package, perception, physics, prefab, probuilder, project, sample, scene, script, scriptableobject, shader, smart, terrain, test, timeline, ui, uitoolkit, validation, xr — **verify actual list via `ls recipes/`**):
  - Pick one representative recipe (preferably one using at least one `_shared/*` helper).
  - Concatenate its declared `## Prerequisites` helpers with its `CommandScript` code block.
  - Execute via `mcp__unityMCP__Unity_RunCommand`. (The human operator may need to provide a Unity project for recipes that touch scene/asset state.)
  - Record compilation success + execution success in the notes file.
- Any recipe that fails either compilation or execution is filed as a blocker with a `BLOCKER:` tag in the notes. Do **not** mark Task 7 complete while any sampled recipe fails.

**Verification target:**
- Every category's sampled recipe compiles and executes successfully.
- Failed categories are explicitly listed with reproduction notes, not hand-waved.

---

## Task 8: Fix the skill → recipe discovery surface in every `SKILL.md`

**Intent:** Make sure an AI agent reading a `SKILL.md` can actually find the recipe it needs. The existing `Recipe path rule: ../../recipes/<topic>/<command>.md` line is necessary but not sufficient.

**Files:**
- Modify: every `skills/<topic>/SKILL.md` (per-domain).
- Modify: `skills/SKILL.md` (domain index — the `Recipe Path Rule` section still hardcodes `../../recipes/...`).
- Modify: root `SKILL.md` (Routing Order step 2) and `recipes/README.md` if drift is found.

**Required outcomes:**
- Every per-domain `SKILL.md` ends with a `## RunCommand Templates` section that:
  - Names each command (e.g. `gameobject_create`) and gives the **repo-rooted** path (e.g. `recipes/gameobject/gameobject_create.md`) — not just the `../../recipes/...` relative form.
  - States explicitly: "These are C# templates for `Unity_RunCommand`, not standalone MCP tools. Paste the template and any `## Prerequisites` helpers into a single `Unity_RunCommand` call."
- The `## Skills` / command table in each per-domain `SKILL.md` adds a visual cue (e.g. a `Recipe` column with the recipe filename) so an agent scanning the table sees the recipe pointer immediately.
- `skills/SKILL.md` `Recipe Path Rule` section is rewritten to give both a repo-rooted form (`recipes/<topic>/<command>.md`, primary) and the relative form (`../../recipes/<topic>/<command>.md`, fallback), and to note the new `## Prerequisites` declaration on every recipe.
- Root `SKILL.md` Routing Order step 2 ("Topic skill + exact recipe") is expanded to say the recipe lists its own Prerequisites and the agent must concatenate those helper classes before issuing `Unity_RunCommand`.
- `recipes/README.md` adds a section naming the available `_shared/*` helpers so agents who land in `recipes/` instead of `skills/` still learn about them.

**Verification target:**
- A cold-start AI session (no prior memory of this repo) can read `skills/scene/SKILL.md`, land on `scene_load`, find the recipe at `recipes/scene/scene_load.md`, see its Prerequisites, and issue a single correct `Unity_RunCommand` call. Demonstrate this manually on at least three topic skills of different shapes (one simple, one with helpers, one async).

---

## Task 9: Align root `SKILL.md`, `README.md`, and `recipes/README.md` with post-repair reality

**Intent:** Commit `dfde255` already removed `MIGRATION.md` and `agent.md` and consolidated framing into root `SKILL.md` + `README.md`. This task makes sure the **remaining** framing docs match the post-Tasks-1-8 state — no aspirational "verified C# snippet" claims, and correct pointers to the new `_shared/*` helpers and `## Prerequisites` convention.

**Files:**
- Modify: root `SKILL.md`
- Modify: `README.md`
- Modify: `recipes/README.md`
- Modify: `recipes/_shared/README.md` (exists per root `SKILL.md` reference — create if missing)

**Required outcomes:**
- Root `SKILL.md` "Routing Order" and "Other root files" sections explicitly name every `_shared/*` helper that ships now (`execution_result.md`, `workflow_manager.md`, `validate.md`, `gameobject_finder.md`, and — if retained — `skills_common.md`). No helper listed there may be absent from the filesystem (Integrity Check block already enforces this on the agent side).
- Root `SKILL.md` "Overview" / "Integrity Check" sections do not make "every recipe verified" claims beyond what Task 7 actually covers. If Task 7 smoked one recipe per category, the wording says exactly that.
- `README.md` "What's in here" bullet for `recipes/_shared/` enumerates the helpers by name and one-line purpose.
- `recipes/README.md` "Shared Helpers" section matches the actual `_shared/*.md` filenames post-Tasks-2-4 (current text mentions `skills_common` which may end up dropped per follow-up note — reconcile).
- `recipes/_shared/README.md` exists and is a simple index: one row per helper with filename, one-line purpose, and which recipes depend on it (or a grep hint).
- No doc references the deleted `MIGRATION.md` or `agent.md`. Run `rg -l "MIGRATION\.md|agent\.md"` across the repo; only acceptable hit is inside the `docs/superpowers/plans/` historical records.

**Verification target:**
- `rg -n "MIGRATION COMPLETE|verified C# snippet" .` returns zero hits outside `docs/superpowers/plans/`.
- `rg -n "MIGRATION\.md|agent\.md" -g '!docs/superpowers/plans/*' -g '!docs/superpowers/notes/*'` returns zero hits.
- Every `_shared/*.md` file listed in root `SKILL.md` and `README.md` exists on disk.

---

## Task 10: Final audit sweep and plan-exit notes

**Intent:** Close the loop. Re-run the full dependency audit, verify indexes match filesystem reality, and confirm zero hard-breakage symbols remain.

**Files:**
- Append to: `docs/superpowers/notes/2026-04-21-recipes-compile-readiness-notes.md`

**Required outcomes:**
- Re-run the audit commands from Task 0. Every broken-dependency count that was >0 at the start is now either 0 **or** every non-zero hit is an intentional reference inside `recipes/_shared/` itself.
- The 13 original async recipes no longer reference `AsyncJobService` (they have been redesigned per Task 5).
- The `_shared/validate.md` file no longer contains `bool true = default;` or `/* Original Logic:` placeholders.
- **Index coherence check (domain-level only):** `ls -d skills/*/` must match the domain rows in root `SKILL.md`'s domain map AND in `skills/SKILL.md`'s internal index. Zero orphan directories (empty skill folders not listed in either index), zero dangling index rows (listed domains whose `skills/<name>/` directory doesn't exist). No recipe-file listing is expected in either index — individual recipes live under `recipes/<domain>/` and are discovered from the domain's `SKILL.md`, not from the library root.
- Same coherence check for `recipes/_shared/README.md` (lists helpers) vs `ls recipes/_shared/*.md` — no helper mentioned that isn't on disk, no helper on disk that isn't mentioned.
- Same check for `mcp-tools.md` tool names vs tools actually loadable via `ToolSearch` — a tool listed in the matrix but not surfaced by the MCP server is a stale index entry.
- Notes file has a final summary entry listing: total files modified, total recipes redesigned, smoke-test coverage percentage, any outstanding minor issues deferred to a later pass.

**Verification target:**
- Closing audit table in notes matches reality. Zero index drift across root `SKILL.md` / `skills/SKILL.md` / `recipes/_shared/README.md` / `mcp-tools.md`.
- A cold-start AI session opens a randomly selected recipe and successfully executes it end-to-end without the human needing to explain the repo's architecture.

---

## Plan Self-Review

### Coverage

- **Hard breakage (299 + 174 + 156 + 113 + 22 + 13 affected recipes):** Tasks 1–5
- **Discovery breakage (every `SKILL.md`):** Task 8
- **Trust breakage (`MIGRATION.md`, `agent.md`):** Task 9
- **Regression safety:** Task 7 (compile smoke per category) + Task 10 (full re-audit)
- **Dependency cross-reference gap (recipes do not declare which `_shared/*` they need):** Task 6

### Risks

- **`WorkflowManager` original behavior may be deeper than just `SnapshotObject`.** Task 3 requires upfront enumeration of every `WorkflowManager.*` call before writing the shim. If enumeration reveals behavior that cannot be mapped cleanly to standard Unity APIs, the plan must stop and either (a) accept the feature loss and call sites get rewritten, or (b) extend the shim. Do not guess.
- **Newtonsoft.Json might not be installed in every consumer project.** Task 2's shim assumes `com.unity.nuget.newtonsoft-json` is present. The shared file must document this assumption prominently so consumers know to add the package if their project doesn't have it.
- **The 13 async recipes require actual Unity API work** (not just doc fixes). Task 5 is the highest-effort single task in this plan. Budget it accordingly.
- **`TestRunnerApi` may behave differently across Unity versions.** Task 5's test recipe rewrites should document the minimum Unity version they require and fall back to `ExecuteMenuItem` for broader compatibility.
- **Changing the recipe front matter (adding `## Prerequisites`) is a 400+ file edit.** Scripted application is required. Manual edits at that scale will drift.

### Known non-goals (restated)

- No C# asmdef, no plugin, no runtime DLL.
- No mass rewrite of `result.SetResult` call sites — use the extension-method shim.
- No attempt to emulate stateful async via paste-in helpers.
- No retuning of recipe logic beyond what's needed to compile.

### Follow-up notes (post-slice)

- After this plan lands, consider a second pass that audits each `## Prerequisites` declaration against the actual `using ...` directives in the recipe's C# block. Missing `using Newtonsoft.Json;` or `using System.Collections.Generic;` is a different compile error class than missing helpers, and it deserves its own audit pass.
- The `_shared/skills_common.md` file is actively used by ~13 recipes across scriptableobject, optimization, perception, test, shader, and importer (`SkillsCommon.GetAllLoadedTypes`, `Utf8NoBom`, `GetTriangleCount`). Treat it as a first-class shared helper alongside the others; include it in Task 6's `## Prerequisites` script and Task 9's `_shared/README.md` enumeration. (Earlier plan draft incorrectly said `SkillsCommon` had zero callers; that was an audit miss.)
- After compile-readiness is restored, a "one recipe per command actually exercises its happy path" PlayMode-style coverage pass would be valuable, but it is out of scope here.

### Execution note

- Tasks 1–4 are near-independent and can be parallelized across subagents. Task 5 must happen serially with its own scoped subagent. Task 6 depends on Tasks 1–4 being done (since it declares the prerequisites those tasks produce). Tasks 7 and 10 are main-agent owned (verification gates).

---

## Session 2 scope (appended 2026-04-21 evening)

Session 1 closed with ext 484/484, pre 484/484, comp 19/484, run 1/484. Mid-session work revealed scope the original Tasks 0–10 did not cover. This section defines the remaining work; the revised execution order at the end of this section replaces the Tasks 0–10 wave for anything not already done.

### Session 2 progress checkpoint

Live gate state: **ext 461/484, pre 461/484, comp 20/484, run 1/484, retired 23/484**.

Tasks completed in session 2 so far:

- **Task 19 ✓** — tracker `R` state + `--retire-all` flag + retired counter; `tracker_next.py` hides R rows. Commit `f92a211`.
- **Task 11 ✓** — `transform_returns` lambda-scope fix via brace-stack depth tracking. Three affected recipes re-extracted (`shader_list`, `component_list`, `component_get_properties`). `shader_list` comp-smoked green to confirm. Commit `f75993d`.
- **Task 12 ✓** — MCP retirement mapping cross-checked against `mcp-tools.md`; 4 missing packages confirmed; deprecation replacements web-confirmed with source URLs in notes. Commit `4c36558`.
- **Task 13 ✓** — 23 recipes deleted (`package/*` 11, `sample/*` 8, `asset/batch_query_assets`, `camera/camera_screenshot`, `console/console_get_logs`, `console/console_clear`). `recipes/package/`, `recipes/sample/`, `skills/sample/` directories removed. Six skill files (`asset`, `camera`, `console`, `scene`, `package`, `script`) now name MCP tools directly, no retirement narrative. Commits `9e4f3a6` → `a30f2c6` → `698a45b`.

**Next up (revised execution order below): Task 14 — install 4 packages + drop compat shims + rewrite 2 navmesh recipes.**

### Mid-session findings that change the plan

1. **REST-era plumbing rejected, not ported.** Upstream `BatchExecutor<T>.Execute`, `SkillResultHelper.TryGetError`, and Newtonsoft.Json-based deserialization are REST-call glue with no purpose in stateless `Unity_RunCommand`. `*_batch` recipes are rewritten as `foreach` loops taking typed arrays.
2. **Version-compat shims rejected.** Upstream `CinemachineAdapter` (562 lines; Cinemachine 2↔3 shim) and `XRReflectionHelper` (558 lines; XRI 2↔3 reflection shim) are not ported as `_shared/*.md`. Repo commits to Cinemachine 3, XRI 3, and direct v3 API use.
3. **Native MCP coverage replaces entire domains for retirement.** `package/*` (11 recipes), `script/*`, `asset/batch_query_assets`, `camera/camera_screenshot`, `sample/*` (8), `console/console_get_logs`, `console/console_clear` duplicate first-class MCP tools and are retired. Recipe files are deleted; the owning `skills/<domain>/SKILL.md` carries the routing directly. A skill file that offers no information beyond redirecting elsewhere (`skills/sample/` was the only such case) is also deleted.
4. **Extractor transform bug.** `tools/reextract_recipes.py` `transform_returns` converts `return <expr>;` inside `Select`/`Where` lambda bodies — invalid, since the lambda must return a value. Caught on `shader_list`, `component_list`; likely affects more of the 73 recipes using Select-block-lambdas.
5. **Unity project package inventory established.** Installed: timeline, inputsystem, test-framework, newtonsoft-json, ugui, modules.ai, modules.terrain, modules.animation, modules.physics, render-pipelines.universal, ide.visualstudio, and more. Missing: `com.unity.cinemachine`, `com.unity.xr.interaction.toolkit`, `com.unity.probuilder`, `com.unity.ai.navigation`.
6. **Unity 6000+ is the only supported baseline.** No back-compat branches. `#if UNITY_6000_0_OR_NEWER` conditionals are dropped in favor of the new-API-only path.

### Locked decisions (do not revisit without evidence)

- Do not port `CinemachineAdapter`, `XRReflectionHelper`, `BatchExecutor`, `SkillResultHelper` as `_shared/*.md`.
- Retired recipe files are deleted. The owning `skills/<domain>/SKILL.md` carries the routing directly (names the MCP tool or points at the replacement recipe inline). `.md` files outside `docs/` are post-mortem only — no tombstones, no redirect files, no dated retirement narratives, no "this recipe is a redirect" meta-text.
- Delete a `skills/<domain>/SKILL.md` only when every capability it claims is already covered by another skill or a native MCP tool. Mixed skills (some recipes retired, others active) stay; their routing is corrected in-place.
- **Retirement requires a functional-equivalence check against the real MCP tool schema, not just a one-liner match in `mcp-tools.md`.** Load the tool via `ToolSearch select:<name>`, compare its parameters against the recipe's signature, and confirm every recipe capability (parameter surface, persistence semantics, return shape) is covered. If the MCP tool is a strict subset, keep the recipe. `camera_screenshot` → `Unity_Camera_Capture` was retired on a one-line-description match and reversed after schema check (the MCP tool is in-memory-only, takes instanceID alone, no file persistence, no custom resolution).
- Do not replace a deprecated API based on model-memory alone. Web-confirm the replacement + semantics against Unity's official docs; record source URL in the notes file before applying.
- Do not add back-compat to older Unity versions. Unity 6000+ is the baseline; root `README.md` states this once.
- **After any domain's recipes are fully comp-green (all its non-retired rows at `comp:x`), run `writing-skills` against that domain's `skills/<domain>/SKILL.md` and apply every recommendation before moving to the next domain.** This is the gate that keeps the skill surface coherent with what the recipes actually do.

### Task 11: Fix `reextract_recipes.py` lambda-scope `return` transform

**Intent:** the current `transform_returns` tokenizer cannot distinguish a `return <expr>;` inside a `Select`/`Where`/`.Where(x => { ... })` lambda body from a top-level statement. Wrapping the former in `{ result.SetResult(<expr>); return; }` breaks the lambda's value-return contract and shadows `result` when paired with the `(expr)` form.

**Files:** Modify: `tools/reextract_recipes.py`.

**Required outcomes:**
- Tokenizer tracks `=> {` openings as a lambda-scope marker. A `return <expr>;` inside an active lambda scope is emitted unchanged; only returns outside lambda scope get the `SetResult` rewrite.
- Scripted audit: for every recipe whose csharp block contains `=> {`, the post-extraction body must compile against a stub `CommandScript` (dry compile via `python3 -m mypy`-equivalent C# tool, or reflection-smoke in Unity).
- Re-run extractor on the recipes the original pass affected. Tracker `ext` cell is re-verified per recipe.

**Verification target:** `shader_list`, `component_list` extract cleanly. No `{ result.SetResult(...); return; }` wrappers inside lambda bodies anywhere in `recipes/`.

### Task 12: Session-2 pre-flight

**Intent:** establish ground truth before mutating anything.

**Files:** Modify: `docs/superpowers/notes/2026-04-21-recipes-compile-readiness-notes.md`.

**Required outcomes:**
- Read and annotate `mcp-tools.md`. For every retirement candidate in Task 13, record the line-item match. **No retirement without a confirmed match.**
- Re-run `UnityEditor.PackageManager.Client.List(true, true)` via `Unity_RunCommand`; confirm the "missing 4" inventory.
- Web-search Unity's official upgrade guide / scripting API docs for each deprecation candidate surfaced so far:
  - `FindObjectsOfType<T>()` → replacement name + `FindObjectsSortMode` enum values + whether sort mode is required vs optional.
  - `FindObjectOfType<T>()` → replacement name + any behavioral difference.
  - `UnityEditor.AI.NavMeshBuilder.BuildNavMesh()` / `.ClearAllNavMeshes()` → confirm whether `[Obsolete]` attribute is actually applied in Unity 6000+, and the `NavMeshSurface` workflow details (single-surface vs multi-surface, how to match legacy "bake whole scene" semantics).
- Record source URLs (e.g. `https://docs.unity3d.com/6000.2/...`) in the notes file alongside each confirmation. Any replacement that cannot be web-confirmed is deferred, not applied from memory.

### Task 13: Retire-to-MCP (delete recipes, route via skills)

**Intent:** remove recipes that duplicate native Unity MCP tools. The owning skill file carries the routing directly — no tombstone recipe files.

**Files:**
- Delete: each retired recipe `.md`.
- Modify: `skills/<domain>/SKILL.md` for every domain with retirements — the skill itself names the MCP tool and its usage.
- Modify: `skills/SKILL.md` (internal index) and root `SKILL.md` (domain map) to drop any entry for a fully-retired domain (including its directory, e.g. `skills/sample/`) whose capability is covered elsewhere.
- Delete: a `SKILL.md` whose only purpose is forwarding — treat "fully useless" as "carries no info another skill doesn't already carry." Mixed skills (asset / camera / console) stay but drop references to the deleted recipes; they mention the native MCP tool inline as the primary route.
- Modify: tracker. Retired recipes use the `R` cell value (Task 19). Tracker rows remain as a historical record — tracker lives in `docs/` where dated entries are allowed.

**Required outcomes:**
- Every retired recipe file is deleted. External links can still route via the skill.
- No new redirect / tombstone files are created outside `docs/`.
- The owning `skills/<domain>/SKILL.md` contains a post-mortem statement of what to do (e.g. "For Game Camera screenshots, use the native `Unity_Camera_Capture` tool.") with no "retired 2026-04-21" framing, no "this recipe is a redirect" narrative, no dates.
- Fully-redundant skills are deleted entirely. Partially-retired skills stay with corrected content.

**Retirement candidates (require Task 12 web-confirm + mcp-tools.md match):**
- `package/*` (11) → `Unity_PackageManager_ExecuteAction`, `Unity_PackageManager_GetData`.
- `script/*` → `Unity_CreateScript`, `Unity_DeleteScript`, `Unity_FindInFile`, `Unity_ScriptApplyEdits`, `Unity_ValidateScript`. (No recipe files exist for this domain — only the skill needed updating.)
- `asset/batch_query_assets` → `Unity_FindProjectAssets`.
- `camera/camera_screenshot` → `Unity_Camera_Capture`.
- `sample/*` (8 recipes) → same-repo duplicates of `recipes/gameobject/*` and `recipes/scene/*`. Whole `skills/sample/` directory also deleted: it held no info the gameobject / scene skills don't already cover.
- `console/console_get_logs`, `console/console_clear` → `Unity_GetConsoleLogs`, `Unity_ReadConsole`.

**Verification target:** a cold-start AI reading any updated `SKILL.md` knows which MCP tool to call without needing a recipe file.

### Task 14: Install 4 packages, drop compat shims, rewrite 2 navmesh recipes

**Files:**
- Install: `com.unity.cinemachine` (v3.x), `com.unity.xr.interaction.toolkit` (v3.x), `com.unity.probuilder`, `com.unity.ai.navigation`. Use `UnityEditor.PackageManager.Client.Add` via `Unity_RunCommand`.
- Modify: all `recipes/cinemachine/*.md` — remove `CinemachineAdapter.*` references; use direct `CinemachineCamera` API.
- Modify: all `recipes/xr/*.md` — remove `XRReflectionHelper.*`; drop `#if !XRI` gates; use direct XRI 3.x API.
- Modify: all `recipes/probuilder/*.md` — drop `#if !PROBUILDER` gates; remove `NoProBuilder()` fallbacks.
- Modify: `recipes/navmesh/navmesh_bake.md`, `recipes/navmesh/navmesh_clear.md` — use `NavMeshSurface.BuildNavMesh()` / `.RemoveData()` component API. Find-or-add `NavMeshSurface` components on scene roots to preserve "bake whole scene" semantics.
- Modify: `skills/cinemachine/SKILL.md`, `skills/xr/SKILL.md`, `skills/probuilder/SKILL.md`, `skills/navmesh/SKILL.md` — add `## Requirements` block naming the required package + version.

**Verification target:** one representative recipe per domain smokes green at comp gate. Package-install is recorded in tracker notes.

### Task 15: Rewrite `*_batch` recipes as `foreach` loops

**Files:** Modify: every recipe ending in `_batch.md` that currently calls `BatchExecutor.Execute<T>(...)` or references `SkillResultHelper`.

**Required outcomes:**
- Parameter shape changes: `string items` (JSON blob) becomes `T[] items` (typed array matching the item struct).
- Body is a straight `foreach (var item in items) { ... }` loop. Per-item errors are captured into a `List<object>` results array; aggregated success/fail counts go into the final `result.SetResult(...)`.
- No `JsonConvert.DeserializeObject` calls.
- The non-batch sibling's logic is inlined inside the loop body, or extracted into a private static method within the same `CommandScript` class if it's long.

**Verification target:** `material_assign_batch`, `event_add_listener_batch`, `uitk_create_batch`, `prefab_instantiate_batch`, `gameobject_set_transform_batch` compile + execute with the new shape.

### Task 16: Inline private upstream helpers per recipe

**Files:** Modify: recipes calling upstream private static methods that were not ported as `_shared/*.md`.

**Required outcomes:**
- For each such method, paste the upstream body (verbatim from SHA `55b03ef3`) directly into the calling recipe as a `private static` method in the `CommandScript` class.
- Do not create new `_shared/*.md` files. These are recipe-local helpers.

**Scope as of 2026-04-21:**
- `FindShaderByNameOrPath` → 1 recipe (`shader_check_errors`).
- `GetSimilarTypes`, `AllowMultiple` → 1 recipe (`component_add`).
- Any others surfaced by Task 18 reflection sweep.

### Task 17: Unity 6000+ commitment + web-confirmed deprecation replacement

**Files:**
- Modify: `recipes/_shared/gameobject_finder.md` — drop `#if UNITY_6000_0_OR_NEWER` branches; keep only the new-API path.
- Modify: 3 recipes with `#if UNITY_*` — `recipes/physics/physics_set_material.md`, `recipes/physics/physics_create_material.md`, `recipes/uitoolkit/uitk_get_panel_settings.md`. Collapse to the new-API path.
- Modify: `README.md` — add `**Requires:** Unity 6000+` once.
- Modify: recipe bodies using deprecated APIs, per Task 12 web confirmations.

**Required outcomes:**
- No `#if UNITY_` conditionals remain in `recipes/` (including `_shared/`).
- `FindObjectsOfType<T>()` → `FindObjectsByType<T>(FindObjectsSortMode.None)` across 2 recipes, after Task 12 confirms the exact call signature.
- `FindObjectOfType<T>()` → `FindFirstObjectByType<T>()` across 9 recipes, after Task 12 confirms.
- Source URL from Task 12's web confirmation is referenced in the notes file per replacement, not inline in recipes.

**Verification target:** grep for `#if UNITY_`, `FindObjectsOfType`, `FindObjectOfType` returns zero matches in `recipes/` (excluding `Resources.FindObjectsOfTypeAll` which is a different, non-deprecated API).

### Task 18: Reflection-based `[Obsolete]` sweep

**Intent:** catch deprecations missed by grep + memory.

**Required outcomes:**
- One `Unity_RunCommand` script iterates every rendered recipe, attempts to resolve every referenced type and member, and reports any `ObsoleteAttribute` hits. OR: enable `-warnaserror` for the comp smoke and let Unity's compiler surface deprecations as build failures.
- Every new deprecation found is handled per Task 17 (web-confirm → replace). Recipes that fail comp because of a newly-found deprecation become `comp:B` pending confirmation.

### Task 19: Tracker `R` (retired) state

**Files:** Modify: `docs/superpowers/notes/recipe-validation-tracker.md` legend; `tools/tracker_update.py`; `tools/tracker_next.py`.

**Required outcomes:**
- Tracker legend gains `R` = retired with a redirect target recorded in the notes column (MCP tool name or other recipe path).
- `tracker_update.py` accepts `R` as a valid value (current acceptance list is `x`/`-`/`B`).
- `tracker_next.py` skips rows where any gate is `R` — they are not pending work.
- Summary counters at top of tracker add a `retired: N / total` line alongside the existing gate counters.

**Verification target:** `python3 tools/tracker_update.py package_list comp R --note "retired → Unity_PackageManager_GetData"` updates the cell and the summary. `tracker_next.py --gate comp` does not surface that recipe.

### Task 20: Re-smoke comp gate across all domains

**Required outcomes:**
- `tools/render_recipe.py` renders every non-retired recipe.
- `Unity_RunCommand` comp-smokes each (body in `if (false)`). Target: every recipe is `comp:x` or `R`. Remaining `B` cells are documented individually with reason.
- Existing session 1 `B` cells are re-verified per the disposition table in `2026-04-21-recipes-compile-readiness-notes.md` §4.
- **Per-domain completion gate:** as each domain hits "every non-retired row at `comp:x`", run `writing-skills` against `skills/<domain>/SKILL.md` and apply its recommendations before moving to the next domain. Record the sweep outcome in the tracker notes for the last recipe of that domain.

### Task 21: Selective `run` gate

**Required outcomes:**
- `run:x` verified for read-only recipes: queries that don't create, delete, or mutate scene/asset state. Target set includes most `*_get_*`, `*_list`, `*_find*`, `*_check_*` recipes.
- Recipes that mutate state stay `run:-` with a "fixture required" note. A follow-up plan seeds fixtures; not in this cycle.

### Task 22: Recipe-level cleanliness sweep

**Intent:** every recipe under `recipes/` should obey CLAUDE.md's "post-mortem only, no prose that re-explains code" rule. The extraction pipeline pulled real upstream logic but left per-recipe prose duplication and an overlong `## Prerequisites` block. Clean this up once recipes are compile-validated so the surface is tight before the library ships.

**Files:**
- Modify: every non-retired recipe `.md` under `recipes/<domain>/`.
- Modify: `tools/inject_prerequisites.py` (template update).
- Modify: `recipes/_shared/*.md` — **prose only**. The `csharp` fenced code blocks inside helpers (MiniJson, GameObjectFinder, Validate, WorkflowManager, ComponentSkills split, SkillsCommon) stay byte-identical because they're working + confirmed. Headers, "Call pattern" sections, "Do not" lists, and paste-instructions inside helper files are audited for redundancy and stripped where duplicated elsewhere.

**Required outcomes:**
- **Compact `## Prerequisites` block** — replace the multi-line header + bullets with a single line per recipe:
  ```
  **Prerequisites:** [`execution_result`](../_shared/execution_result.md), [`validate`](../_shared/validate.md), [`gameobject_finder`](../_shared/gameobject_finder.md)
  ```
  Applied via `inject_prerequisites.py` template change + full re-run. Idempotent. Helpers unchanged.
- **Drop prose that the code already expresses.** If a recipe has a `## Parameters` table AND inline `// description` comments for the same locals at the top of `Execute`, keep one (prefer the inline comments — paste-ready). If a recipe has a `## Returns` prose schema AND a `result.SetResult(new { ... })` that makes the shape obvious, drop the prose.
- **Drop "Notes" / "Why" tail sections** whose content is either obvious from the code or already restated in the owning SKILL.md. Keep a note only when it captures a non-obvious gotcha that the code can't express (e.g., "this call silently no-ops outside `EditorApplication.update` on Windows").
- **Drop any remaining "Use this command when…" / "For quick X, prefer Y" choice-narrative** if the owning SKILL.md's routing section already carries the guidance. Duplication is the tax to remove.
- **Do not shrink any `csharp` code block.** Compile-validated recipes AND `_shared/*.md` helpers stay byte-identical in their `csharp` fence. This task is a prose pass only.
- **`_shared/*.md` prose is in scope, `_shared/*.md` code is out of scope.** Redundant headers / duplicate "paste this inside the same code block as `CommandScript`" reminders get trimmed; the actual C# body is untouched.

**Scripted safeguards:**
- Before committing, re-render one recipe per domain and confirm its code block hash matches pre-Task-22. Hash mismatch → stop and investigate.
- Re-run `python3 tools/tracker_next.py --gate comp` to confirm zero regressions (no previously-green recipes slip to pending).

**Verification target:**
- Median recipe length drops ≥30% (measured via `wc -l recipes/<domain>/*.md` before and after, excluding `_shared/` and README).
- Every `comp:x` row stays `comp:x`. No recipe's `csharp` block content changes.
- `grep -rn "## Parameters$\|## Notes$\|## Returns$" recipes/ | wc -l` drops substantially (exact threshold recorded in the notes file).

### Revised execution order (replaces Tasks 0–10 wave for remaining work)

1. ~~**Task 19**~~ ✓ tracker `R` state + tool updates.
2. ~~**Task 11**~~ ✓ extractor lambda bug fixed.
3. ~~**Task 12**~~ ✓ pre-flight complete; retirement mappings + deprecation URLs in notes.
4. ~~**Task 13**~~ ✓ retire-to-MCP.
5. ~~**Task 14**~~ ✓ packages installed, compat shims dropped, navmesh rewritten.
6. ~~**Task 15**~~ ✓ `*_batch` → `foreach` rewrite.
7. ~~**Task 16**~~ ✓ inline private upstream helpers (+ `_shared/project_skills.md` / `_shared/perception_helpers.md` / `_shared/component_type_finder.md` / `_shared/value_converter.md` / `_shared/skills_common.md` created to cover the 6 domains that shared helper surfaces).
8. ~~**Task 17**~~ ✓ Unity 6+ commit + deprecations applied.
9. ~~**Task 5**~~ ✓ test / package / editor async split (including `test_list` / `test_list_categories` follow-up).
10. ~~**Task 18**~~ ✓ reflection-based obsolete sweep (enumeration captured; any findings folded into Task 17).
11. **Task 21** ← **NEXT** — extended selective run gate. Run-verify eligible read-only recipes (`*_get_*`, `*_list`, `*_find*`, `*_check_*`) that are already `comp:x`. Recipes that mutate stay `run:-` with "fixture required" note. Runs before the final comp re-smoke so `run:x` states are factored into any last-round comp findings.
12. **Task 20 + 22 combined** — full comp re-smoke across every non-retired recipe. Rewrite prose (`## Prerequisites` → single line; drop duplicated `## Parameters` / `## Returns` / `## Notes`) in the same pass to avoid a second smoke round. Per-domain `writing-skills` audit as each domain hits fully comp-green. Code blocks and `_shared/*.md` csharp bodies stay byte-identical.
13. **Task 10** — final audit + plan-exit notes.

