# Recipes compile-readiness — session notes

State as of 2026-04-21. Accompanies `docs/superpowers/plans/2026-04-21-recipes-compile-readiness-repair-plan.md`.

## Gate progress

| Gate | Start | End | Notes |
|---|---:|---:|---|
| ext  | 403 | **484/484** | 81 `Original Logic:` + 44 `UnitySkillsBridge.Call` stubs re-extracted from upstream SHA `55b03ef3`. |
| pre  | 1 | **484/484** | Scripted via `tools/inject_prerequisites.py`; detector scans recipe csharp blocks for helper symbols. |
| comp | 1 | **19/484** | One representative per domain across 19 of 34 domains. |
| run  | 1 | 1/484 | Out of session scope. |

## Tooling changes this session

- `tools/reextract_recipes.py`
  - Removed dead `detect_multiline_return` call (undefined helper).
  - Wrapped the `return <expr>;` → `result.SetResult(<expr>); return;` transform in `{ … }`. Without braces, a `return;` emitted from the body of a braceless `if`/`else`/`case` escapes the conditional and runs unconditionally (caught on `component_add`, which silently no-op'd before the brace fix).
  - Added a second stub shape: `UnitySkillsBridge.Call(...)` placeholder. For these recipes no preamble is preserved — the stub is a single call.
- `tools/inject_prerequisites.py` (new) — scripted `## Prerequisites` detector/injector. Scans the csharp block for `result.SetResult`, `Validate.*`, `GameObjectFinder|FindHelper.*`, `WorkflowManager.*`, `SkillsCommon.*`, `ComponentSkills.FindComponentType`, `ComponentSkills.ConvertValue`, and maps to `_shared/*.md` paste-ins. Idempotent.
- `tools/render_recipe.py` (new) — concatenates helper paste-ins + recipe csharp into a single Unity_RunCommand payload. `--comp` wraps the Execute body in `if (false)` for compile-only smoke.

## Key findings

### 1. `inject_prerequisites.py` had to end the existing-section span at a ```csharp fence too
Recipes whose csharp fence is not preceded by a `##` heading would otherwise have their code block absorbed into the Prerequisites span on re-run, deleting the recipe body. Reproduced on first normalization pass — 286/484 recipes lost their code. Fix: `find_existing_prereq_span` now ends at the next `##` heading OR the next ```csharp fence, whichever comes first.

### 2. Console recipes used `result.Return(...)`, not `result.SetResult(...)`
All 13 recipes under `recipes/console/` used an idiom that's neither native to `ExecutionResult` nor provided by `_shared/execution_result.md`. Normalized to `result.SetResult(...)` in-place via `sed`.

### 3. Comp-gate blocker patterns (recipe-level, not tool-level)
Each of these requires per-recipe fixup and is out of scope for Task 7:

| Pattern | Example recipe | Affected count (est.) |
|---|---|---|
| Upstream private helper not in `_shared` (`FindShaderByNameOrPath`, `CinemachineAdapter`, `PackageManagerHelper`, `XRReflectionHelper`, `NoXRI`, `NoProBuilder`, `DiscoverTests`, etc.) | `shader_check_errors`, `cinemachine_list_components`, `package_list`, `xr_setup_interaction_manager`, `test_get_summary`, `test_list_categories` | ~40+ |
| `BatchExecutor.Execute<T>(...)` + `SkillResultHelper` from upstream REST layer | `material_assign_batch`, `event_add_listener_batch`, `uitk_create_batch` | ~30 |
| `#if !PROBUILDER` / `#if !XRI` gates + `UnitySkillsBridge` stub residue | probuilder/\*, xr/\* | ~44 (entire domains) |
| `return <expr>;` inside a `Select`/`Where` lambda got transformed into `result.SetResult(...); return;` — valid top-level but invalid as a lambda body | `shader_list` confirmed, likely more across 73 recipes using Select-block-lambdas | undetermined |
| Body references parameters (`sourcePrefabPath`, `name`, etc.) that were never inlined as locals during extraction | `prefab_create_variant`, some xr recipes | ~5 |
| Non-`CommandScript` shape (top-level REPL script + `return`, Newtonsoft.Json) | `batch_query_assets` | 1 |

### 4. Unity_RunCommand occasionally rejected compile-only with "User interactions are not supported"
Hit on `shader_delete` twice in a row (with `if (false)` wrapping, no execution path). Unclear whether it's a dialog-adjacent symbol triggering it at compile-analysis time or a transient Unity state. Deferred; single recipe.

## Comp-gate greens by domain

animator, asset, camera, console, editor, gameobject (pilot), light, navmesh, optimization, physics, project, sample, scene, scriptableobject, shader, smart, terrain, timeline, ui, validation.

## Comp-gate blockers by domain

cinemachine, component (untested — 479-line render too large), event, material, package, prefab, probuilder, test, uitoolkit, xr.

## Tracker entries

Every smoke attempt is recorded in `recipe-validation-tracker.md` with its result and short note. Blockers are marked `B` with the reason.

## Next session — start here

Session 2 scope is in `docs/superpowers/plans/2026-04-21-recipes-compile-readiness-repair-plan.md` as Tasks 11–21 with a revised execution order at the bottom of that doc. Cold-start steps:

1. Read the plan's "Session 2 scope" section and the "Locked decisions" subsection before any edits.
2. Run Task 12 pre-flight first (read `mcp-tools.md`, re-list installed packages, web-confirm deprecations). The pre-flight outputs are required inputs to Tasks 13, 14, 17.
3. Task 19 (tracker `R` state) should land before Task 13 (retirements need the `R` cell value to exist).
4. Follow the revised execution order 1–13 at the bottom of the plan doc.

Do not revisit the locked decisions in the plan doc without evidence: no back-port of `CinemachineAdapter` / `XRReflectionHelper` / `BatchExecutor`, no SKILL.md deletions, no recipe deletions (tombstone only), no memory-based deprecation replacement (web-confirm first).
