# Contributor notes for this repo

This file lives on `dev` only. The publish script does not sync it to `main`,
so it never ships with the skill pack.

## Branch layout

- `main` — installable skill pack. Only `recipes/`, `skills/`, `references/`,
  `README.md`, `SKILL.md`, `mcp-tools.md`, `LICENSE`, and its `.gitignore`.
- `dev` — everything above plus `tools/`, `docs/superpowers/`, `CLAUDE.md`,
  `AGENTS.md`, `Makefile`. All work happens here.

Never merge `dev → main`. Publish via `make publish` (runs `tools/publish.sh`).
The script refuses to run from any branch other than `dev` with a clean tree.
Use `make dry-run` first if you want to see what would change.

## What this repo is

A skill pack that an agent clones into its skills directory (e.g.
`~/.claude/skills/unity-mcp-skills`). Everything the agent sees must be
post-mortem content — the "what works" answer, not the journey to it.

## Writing rule (applies to every `.md` outside `docs/`)

- Post-mortem only. No history, no "we changed X because Y", no rationale
  narratives. Why-we-did-it lives in git log.
- Document what works. Call out a bad pattern only when getting it wrong is
  likely and the failure mode is non-obvious.
- Code blocks must be paste-ready. Prose wraps them; prose does not
  re-explain what the code already says.
- No "Notes" / "Why" tail sections stuffed with context. If a note is truly
  load-bearing, inline it as a one-line comment at the relevant code line.
- Recipes are `IRunCommand` templates verified to compile and run. The
  `_shared/*.md` helpers they declare as Prerequisites are the same.

## SKILL.md is not for editing skills

Root `SKILL.md` and per-domain `skills/<topic>/SKILL.md` teach agents how to
work Unity through the MCP fast and without hallucination. They route to
recipes. They are not contributor docs. Do not put authoring conventions
inside them.

## Where historical reasoning is allowed

- `docs/superpowers/plans/*.md` — planning docs, rationale, trade-offs.
- `docs/superpowers/notes/*.md` — task-by-task execution notes.
- Git commit messages.

Nowhere else.

## Sources of truth

- Upstream reference: `https://github.com/Besty0728/Unity-Skills` at
  SHA `55b03ef32de920f4f2d884c9eed1491a535c2ae5`. Clone to
  `/tmp/upstream-unity-skills` when re-extracting recipe bodies.
- Extraction tool: `tools/reextract_recipes.py`.
- Validation tracker: `docs/superpowers/notes/recipe-validation-tracker.md`.
- Every recipe must pass `Unity_RunCommand` with its declared Prerequisites
  concatenated. No recipe lands in `main` without that validation.

## Session resumption (fresh chat)

Do this to pick up mid-job without carrying prior session context:

1. Read this file.
2. Read the active plan: `docs/superpowers/plans/2026-04-21-recipes-compile-readiness-repair-plan.md`.
3. Do NOT read the tracker whole — it is 500+ rows. Query it:
   `python3 tools/tracker_next.py --gate <ext|pre|comp|run> --limit 5`.
4. For each returned recipe, do the extract → declare prereqs → compile-gate
   → (if safe) run-gate loop. After each success:
   `python3 tools/tracker_update.py <recipe_stem> <gate> x --note "<short>"`.
5. Failures: fix them. There is no line-count limit on a fix — 1 line or 50
   lines, do whatever it takes. Only mark `... <gate> B --note "<what broke>"`
   when you genuinely cannot determine the correct fix yourself (unknown
   dependency, missing upstream type, design ambiguity requiring user input).
   Everything else: fix it inline and re-smoke before moving on.

**Never stop mid-sweep to ask.** Mark B, move to the next recipe, and keep
going until every recipe has been attempted. The sweep ends only when the
tracker has no remaining `comp:-` rows.

Gate priority order: finish all `ext` first, then `pre`, then `comp`, then `run`.
A recipe must earn earlier gates before later ones.

## Unity MCP is a hard dependency while working on this repo

The Unity Editor's official `com.unity.ai.assistant` MCP must be live and reachable. Tools in the `mcp__unityMCP__*` namespace (e.g. `Unity_RunCommand`, `Unity_PackageManager_GetData`, `Unity_ValidateScript`, `Unity_FindProjectAssets`, `Unity_Camera_Capture`) are the primary way to validate recipes, introspect project state, and evaluate retirement candidates.

- If Unity MCP is not connected when you start, **ask the user to connect it before continuing.** Do not try to infer Unity project state from recipe text or upstream sources alone; the live project is ground truth.
- **Before claiming an MCP tool replaces, supersedes, or is the preferred route for any recipe — load the tool schema (`ToolSearch select:<ToolName>`) and confirm the parameter surface covers every capability the recipe provides.** `mcp-tools.md` one-line descriptions are summaries, not contracts. Retiring a recipe on a description match alone is how `camera_screenshot` got wrongly deleted — `Unity_Camera_Capture` turned out to be in-memory-only, accept `cameraInstanceID` only, with no file persistence or custom resolution. Schema check first, retirement/routing second.
- Same rule when writing SKILL.md routing prose: verify the tool name + parameter shape you're about to recommend by loading the schema. A fabricated tool name or wrong parameter hint in a skill file is worse than no mention — agents will paste it verbatim.

## Gotchas (stable quirks of Unity_RunCommand)

Put these in code comments where the gotcha actually bites, not in prose
everywhere. Listed here so a fresh session knows the shape.

- `Unity_RunCommand` auto-wraps code in `namespace Unity.AI.Assistant.Agent.Dynamic.Extension.Editor`.
- `private` nested classes get duplicated to namespace scope by the
  reformatter and fail CS1527. Use `internal` top-level with an underscore
  prefix (see `_GameObjectFinderCache`).
- `BindingFlags.Public | BindingFlags.Instance` triggers an NRE in the
  reformatter. Use `GetProperties()` / `GetFields()` without args.
- The same reformatter NRE also hits when `BindingFlags` is passed to
  `Type.GetMethod(name, flags, binder, types, modifiers)`. Use
  `type.GetMethods()` + a `foreach` filter on `Name` / `IsStatic` /
  `GetParameters()` instead. The 2-arg `GetMethod(name, Type[])` overload
  (no BindingFlags) is also safe.
- **The `using System.Reflection;` directive itself triggers the
  Unity_RunCommand reformatter NRE** (empirically: just declaring the
  directive with NO reflection usage in the body still fails with
  `UNEXPECTED_ERROR: Object reference not set to an instance of an object`
  and no compile log). Do not declare it. Fully-qualify every reflection
  type instead: `System.Reflection.MethodInfo`, `System.Reflection.FieldInfo`,
  `System.Reflection.PropertyInfo`, `System.Reflection.BindingFlags.X`.
  Earlier notes that said "the directive itself is fine" were wrong —
  confirmed 2026-04-22 across event/* and scriptableobject/* recipes.
- Same NRE hits a `catch (ReflectionTypeLoadException ex)` clause when
  `using System.Reflection;` is in scope. Fully-qualify the exception
  type: `catch (System.Reflection.ReflectionTypeLoadException ex)`.
- `AssetDatabase.DeleteAsset(path)` is rejected by the Unity MCP
  analyzer ("User interactions are not supported") — the token appearing
  anywhere in source kills the whole module, even inside `if (false)`.
  Use `AssetDatabase.MoveAssetToTrash(path)` instead. It's restorable
  from the OS trash, functionally equivalent for recipe use, and
  analyzer-safe. `shader_delete` and `asset_delete_batch` both use this.
- `CompilationPipeline.RequestScriptCompilation()` fails CS0234 on
  short-name resolution because the Unity_RunCommand reformatter wraps
  code in `namespace Unity.AI.Assistant.Agent.Dynamic.Extension.Editor`,
  and C#'s namespace lookup finds `Unity.CompilationPipeline` first
  (doesn't exist). Fully-qualify:
  `UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation()`.
- `System.Xml.XmlDocument` / `XmlNode` / `XmlElement` are not in the
  `Unity_RunCommand` compile context (CS1069, forwarded to `System.Xml.dll`).
  Parse XML with `string.IndexOf` scans instead.
- `System.Text.RegularExpressions.Regex` / `Match` are likewise unavailable
  (CS0103 for `Regex`/`RegexOptions`, CS1069 for `Match`). A `using
  System.Text.RegularExpressions;` directive compiles, but the types resolve
  to nothing usable. Use `string` methods or a hand-written scanner.
- `Newtonsoft.Json` is not available in the dynamic compile context even
  when `com.unity.nuget.newtonsoft-json` is installed. Use the MiniJson
  serializer in `_shared/execution_result.md`.
- `JsonUtility.ToJson` silently returns `"{}"` for anonymous types.
- `(long)((dynamic)obj).fieldName` member access on `dynamic`-cast values
  (typical when summing a field off an `object` produced by `new { ... }`)
  fails the Unity_RunCommand analyzer with `UNEXPECTED_ERROR: Execution
  failed: No logs available` — no compile log, no stack trace. Maintain a
  running total via a typed local (`long totalWasted = 0; totalWasted += …;`)
  instead of computing it with `.Sum(d => (long)((dynamic)d).foo)` at the end.
- `result.SetValue(...)` is NOT a member of `ExecutionResult`. Upstream
  recipes used it; the correct call is `result.SetResult(...)` via the
  `_shared/execution_result.md` extension. Missing the swap ends in
  `CS7036: ... SerializedPropertyExtensions.SetValue<DataSourceType>(...)`
  because the compiler picks up an unrelated `SetValue` extension from
  `UnityEditor` internals.
