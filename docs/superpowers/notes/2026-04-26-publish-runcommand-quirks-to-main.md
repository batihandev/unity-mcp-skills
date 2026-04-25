# Publish `Unity_RunCommand` quirks to main

Status: deferred. Captured 2026-04-26 during a README audit.

## Problem

`CLAUDE.md` lives on `dev` only — `tools/publish.sh` does not sync it. The
`## Gotchas` section inside it documents ~15 reformatter / analyzer quirks of
`Unity_RunCommand` (namespace-wrap NRE on `using System.Reflection;`,
`BindingFlags` NRE, `static readonly HashSet<string>` CS0012, `AssetDatabase.DeleteAsset`
analyzer reject, `Newtonsoft.Json` not in dynamic compile context,
`result.SetValue` vs `result.SetResult`, `CompilationPipeline.RequestScriptCompilation`
namespace-wrap CS0234, etc.). These are empirically discovered, not in any
Unity docs, and they bite during recipe adaptation or fresh `Unity_RunCommand`
authoring.

The validated recipes shipping to `main` already encode the right patterns,
so verbatim recipe consumption on the install pack works. The gap opens at
two routing-order steps in `SKILL.md`:

3. **Closest recipe** — adapt the nearest recipe if no exact match exists.
5. **Fresh `Unity_RunCommand`** — last resort.

In both, the agent writes or modifies code without a recipe to copy. With no
quirks reference on `main`, the agent re-discovers each one in production —
the failure modes are silent (no compile log on the reformatter NRE, analyzer
killing whole modules) so the agent often reaches for the wrong workaround.

## Decision

Publish the quirks to `main`, but selectively. `CLAUDE.md` itself stays
dev-only — it has contributor content (session-resumption protocol,
branch-layout rules, sources-of-truth) that does not belong in the install
pack.

## Plan (when picked up)

1. **Extract** the `## Gotchas` block from `CLAUDE.md` into a new file at
   the repo root: `runcommand-quirks.md`. Keep only the technical
   `Unity_RunCommand` rules an agent needs while authoring or adapting
   code. Drop dev-process framing.

2. **Reference it from main-shipped files:**
   - `SKILL.md` routing step 5 ("Fresh `Unity_RunCommand`") — one-line
     pointer: "before writing fresh code, skim `runcommand-quirks.md`."
   - `mcp-tools.md` next to the `Unity_RunCommand` row in the dedicated-tools
     table — same pointer.
   - Optionally `recipes/_shared/README.md` paste-pattern section, since
     the paste pattern is exactly where the rules apply.

3. **Update `tools/publish.sh`** to include `runcommand-quirks.md` in the
   sync set.

4. **Trim `CLAUDE.md`'s `## Gotchas` section** to a one-line note pointing
   to `runcommand-quirks.md` so contributor docs and ship docs do not drift.

## Out of scope

- Republishing `CLAUDE.md` itself.
- Republishing `docs/superpowers/` plans, notes, or the validation tracker —
  those are dev-process artifacts.

## Why now-ish

The comp-green sweep is effectively closed (457/463 non-retired). The
recipes encode the rules. The next marginal value-add for `main` users is
giving them the rules directly so adaptation and last-resort code paths
stop hitting the same analyzer/reformatter walls the sweep already mapped.
