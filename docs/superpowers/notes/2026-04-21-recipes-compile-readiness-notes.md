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

## Task 12 pre-flight outputs (2026-04-21)

### MCP retirement mapping (confirmed against `mcp-tools.md`)

| Retiring recipe(s) | Native MCP replacement | `mcp-tools.md` line |
|---|---|---|
| `package/*` (11) | `Unity_PackageManager_ExecuteAction` (Add/Remove/Embed/Sample), `Unity_PackageManager_GetData` | 34–35 |
| `script/*` | `Unity_CreateScript`, `Unity_DeleteScript`, `Unity_FindInFile`, `Unity_ListResources`, `Unity_ScriptApplyEdits`, `Unity_ValidateScript`, `Unity_GetSha` | 23–29 |
| `asset/batch_query_assets` | `Unity_FindProjectAssets` | 38 |
| `camera/camera_screenshot` | `Unity_Camera_Capture` | 32 |
| `console/console_get_logs` | `Unity_GetConsoleLogs` | 22 |
| `console/console_clear` | `Unity_ReadConsole` (has clear flag) | 21 |
| `sample/*` (8) | *in-repo* — duplicates `recipes/gameobject/*`; not MCP | n/a |

MCP tools NOT used as retirement targets because their use case is narrower than the matching recipe's surface: `Unity_AssetGeneration_*`, `Unity_ImportExternalModel`, `Unity_AudioClip_Edit`, `Unity_SceneView_Capture*`, `Unity_Profiler_*`. These tools convert/generate specific asset transformations (texture→material, sprite→animation, etc.) rather than the procedural property-set / list operations the recipes expose.

### Package inventory (verified 2026-04-21 via `Client.List`)

**Installed, recipe-relevant:** `com.unity.timeline` 1.8.12, `com.unity.inputsystem` 1.19.0, `com.unity.test-framework` 1.6.0, `com.unity.nuget.newtonsoft-json` 3.2.2 (note: unavailable in `Unity_RunCommand` dynamic compile), `com.unity.render-pipelines.universal` 17.4.0, `com.unity.ide.visualstudio` 2.0.27, built-in modules: `animation`, `terrain`, `ai`, `physics`, `ugui`, `uielements`.

**Missing — must install:** `com.unity.cinemachine` (34 recipes), `com.unity.xr.interaction.toolkit` (22), `com.unity.probuilder` (22), `com.unity.ai.navigation` (2).

### Deprecation replacements (web-confirmed)

| Deprecated | Replacement | Semantics preserved? | Source |
|---|---|---|---|
| `Object.FindObjectOfType<T>()` | `Object.FindFirstObjectByType<T>()` | Yes — direct replacement, returns the first match by InstanceID order | Unity Discussions; Medium (Swartout) |
| `Object.FindObjectsOfType<T>()` (no args) | `Object.FindObjectsByType<T>(FindObjectsSortMode.None)` | Faster (no sort). For byte-identical legacy behavior use `FindObjectsSortMode.InstanceID` — none of our recipes depend on sort order, so `.None` is the right call. | Unity Discussions; Unity Scripting API docs |
| `Object.FindObjectsOfType<T>(true)` | `Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)` | Includes inactive objects, no sort | Unity Discussions |
| `UnityEditor.AI.NavMeshBuilder.BuildNavMesh()` | `NavMeshSurface.BuildNavMesh()` from `com.unity.ai.navigation` | **Workflow shift, not 1:1.** Legacy API baked a global scene NavMesh via the "Navigation (Obsolete)" window settings. Replacement is per-`NavMeshSurface` component; scene must have at least one `NavMeshSurface` for a bake to happen. | Unity AI Navigation package docs; NavMeshComponents migration |
| `UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes()` | Iterate `FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None)` and call `.RemoveData()` on each | Workflow shift as above | Unity AI Navigation package docs |

**Recipes affected:**
- `FindObjectOfType<T>()` → **9** recipes (sed-mechanical)
- `FindObjectsOfType<T>()` → **2** recipes (sed-mechanical; each call site needs per-call review for the `(true)` overload)
- `NavMeshBuilder.*` → **2** recipes (`navmesh_bake`, `navmesh_clear`) — per-recipe rewrite, not sed

**Sources:**
- [Unity Discussions — "Was FindObjectsOfType deprecation needed?"](https://discussions.unity.com/t/was-findobjectsoftype-deprecation-needed/1597029)
- [Unity 6: FindObjectByType Guide (Medium, Simon Swartout)](https://medium.com/@simon.swartout/unity-6-findobjectbytype-guide-fcbc312261cf)
- [Unity Scripting API — Object.FindObjectsOfType](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Object.FindObjectsOfType.html)
- [Unity Discussions — New Navigation package](https://discussions.unity.com/t/theres-a-new-navigation-package-in-town/902596)
- [AI Navigation package changelog](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/changelog/CHANGELOG.html)

### Task 12 complete. Ready for Task 13.

## Next session — start here

Session 2 scope is in `docs/superpowers/plans/2026-04-21-recipes-compile-readiness-repair-plan.md` as Tasks 11–21 with a revised execution order at the bottom of that doc. Cold-start steps:

1. Read the plan's "Session 2 scope" section and the "Locked decisions" subsection before any edits.
2. Run Task 12 pre-flight first (read `mcp-tools.md`, re-list installed packages, web-confirm deprecations). The pre-flight outputs are required inputs to Tasks 13, 14, 17.
3. Task 19 (tracker `R` state) should land before Task 13 (retirements need the `R` cell value to exist).
4. Follow the revised execution order 1–13 at the bottom of the plan doc.

Do not revisit the locked decisions in the plan doc without evidence: no back-port of `CinemachineAdapter` / `XRReflectionHelper` / `BatchExecutor`; retired recipes are deleted (no tombstones outside `docs/`); a SKILL.md is deleted only when another skill carries the same info; no memory-based deprecation replacement (web-confirm first).
