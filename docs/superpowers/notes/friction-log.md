# Friction Log

Recurring workflow friction observed across sessions. Each entry includes
evidence of recurrence (≥2 occurrences) and classification.

---

## [2026-05-11] Prerequisite-pasting gap for consuming agents

**What repeated / failed:**
Consuming agents (using the installed skill pack from `main`) must manually
identify, read, and assemble all `## Prerequisites` helper pastes into a
single `Unity_RunCommand` code block. Missing one produces a compile error
that looks like "missing API method" rather than "missing paste."

**First known occurrence:** flagged by an agent in a consuming repo (2026-05-11).
**Prior-session evidence:** `inject_prerequisites.py` and `render_recipe.py`
were both built on the dev side in prior sessions to address exactly this
assembly problem — confirming it was painful enough to tool around already.

**Why costly / risky:**
Silent misdiagnosis. The error surface ("CS0103: method not found") looks like
a Unity API issue, not a missing helper class, so the agent chases the wrong
cause. In practice this blocks execution until the root cause is found.

**Generic family:** dependency-assembly failure / silent prerequisite omission

**Classification:** tool opportunity

**Root cause:** `render_recipe.py` (dev tool) solves this for the developer
but is not published to `main`. Consuming agents have no equivalent.

**Candidate fix:** Document `render_recipe.py` usage more visibly in
`recipes/README.md` for dev workflow. Separately, consider whether a
stripped-down assembler (read-only, no `--comp` mode) belongs in `main`
under `references/` or as a script consuming agents can fetch/run.

**Status:** captured — not yet actioned.

---

## [2026-05-11] Stale Newtonsoft.Json references in recipe templates (L1)

**What repeated / failed:**
Two separate template files contained `using Newtonsoft.Json;` and/or
instructions to install the Newtonsoft package — a package that is unavailable
in `Unity_RunCommand`. Agents copying these templates produce recipes that fail
to compile with a Newtonsoft namespace error.

**Files affected:**
- `recipes/README.md` — Golden Template had `using Newtonsoft.Json;` (fixed 2026-05-11)
- `recipes/_shared/README.md` — Paste Pattern had `using Newtonsoft.Json;` plus
  a full `## Newtonsoft.Json` section claiming `execution_result.md` and
  `validate.md` depend on it (fixed 2026-05-11)

**First known occurrence:** external agent in a consuming repo (2026-05-11).
**Recurrence evidence:** same wrong pattern authored independently in 2 files;
`execution_result.md` itself had a "do not use" warning that was ignored by the
template files, confirming the authoring gap is systemic.

**Why costly / risky:**
Agent gets a compile error on a pattern shown in the official template. The fix
(add package) is wrong and the correct fix (use MiniJson) is non-obvious from
the error message alone.

**Generic family:** stale template / forbidden-import drift

**Classification:** repo/doc/process issue — missing publish-time lint gate

**Missing guardrail:** `make publish` does not check recipe content. A
`grep -rn "^using Newtonsoft" recipes/` check would catch this in seconds.

**Candidate fix:** Add `make lint` target + wire into `tools/publish.sh` as a
pre-publish gate. ~10 lines in Makefile, ~5 lines in publish.sh.

**Status:** L1 — durable record written. Awaiting user decision on `make lint` gate.

---

## [2026-05-13] UI Authoring Loop re-derived per task (L0)

**What repeated:**
Every UI fix task (15+ tasks across a session) re-derived the same 3-step sequence:
compile-wait → `ExecuteMenuItem(authoring menu)` → `ExecuteMenuItem(screenshot)`.
Each subagent independently figured out the `isCompiling` polling pattern.

**First known occurrence:** INEVITABLE v1-ui-launch session 2026-05-13.
**Session count:** 1 session, 15+ occurrences.

**Why costly:** ~6 MCP calls per task. Fragile polling implementations.
**Addressed by:** Task A1 — UI Authoring Loop added to `skills/editor/SKILL.md` and `skills/ui/SKILL.md`.

**Classification:** Tool / skill opportunity. Status: addressed in A1.

---

## [2026-05-13] Quality gate passed with self-derived checklist, missed spec divergence (L0)

**What happened:**
A quality gate loop reported "zero problems" across 6 screens. Checklist items were
derived from prior observations ("button not white") — not from design spec JSX files.
Three screens (HUD, Level Up, End Screen) had critical spec divergence the checklist never caught.

**First known occurrence:** INEVITABLE v1-ui-launch session 2026-05-13. Count: 1 session.

**Why costly:** Multiple subagent fix passes produced false confidence. User caught it immediately.

**Addressed by:** Task A1 — spec-check note added to UI Authoring Loop section.

**Classification:** Process / skill issue. Status: addressed in A1.
