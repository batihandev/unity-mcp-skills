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
5. Failures: `... <gate> B --note "<what broke>"` and continue. Blockers are
   triaged in a follow-up pass.

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
- `Newtonsoft.Json` is not available in the dynamic compile context even
  when `com.unity.nuget.newtonsoft-json` is installed. Use the MiniJson
  serializer in `_shared/execution_result.md`.
- `JsonUtility.ToJson` silently returns `"{}"` for anonymous types.
