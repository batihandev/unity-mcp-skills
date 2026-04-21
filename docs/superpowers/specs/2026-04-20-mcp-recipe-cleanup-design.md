# MCP Recipe Cleanup Design

**Date:** 2026-04-20

## Summary

This design defines a cleanup and hardening pass for the Unity MCP Skills Library so it works as a reusable open-source companion for the official Unity MCP.

The repo will treat recipe-derived `Unity_RunCommand` workflows as first-class guidance when no dedicated official Unity MCP tool covers the requested operation. It will remove obsolete `_REFERENCE.md` files, repair broken extracted recipes by checking the original `Besty0728/Unity-Skills` C# source, and rewrite affected skills so they route agents through `mcp-tools.md`, the correct native MCP tool, or the exact recipe file.

## Problem Statement

The current repo has three consistency problems:

1. Several `skills/*/*_REFERENCE.md` files are direct carryovers from the original repo and still behave like sidecar docs instead of clean MCP-era guidance.
2. Many `recipes/*.md` files are mechanically extracted from legacy C# and contain broken placeholder code, which lowers trust and makes them poor starting points for `Unity_RunCommand`.
3. Several `SKILL.md` files still use vague or stale routing language such as "See `recipes/` directory" or link to modules that no longer exist in this repo.

These problems make the repo weaker than intended: the agent cannot reliably tell whether to use a native MCP tool, an exact recipe, or custom `Unity_RunCommand` authored from scratch.

## Goals

1. Keep recipe-derived `Unity_RunCommand` guidance as a core value of the repo.
2. Make the original repo's working C# implementation the authoritative repair source for migrated recipes.
3. Remove obsolete `_REFERENCE.md` files and relocate meaningful content into `SKILL.md` or exact recipe files.
4. Rewrite selected skills to give explicit MCP routing and exact recipe references.
5. Define a repo-wide fallback rule for cases where no exact recipe exists.
6. Make `mcp-tools.md` the shared source of truth for currently available official MCP tools and their descriptions.

## Non-Goals

1. Recreate the original REST API, Python client, or HTTP routing model.
2. Preserve every original advisory module from the upstream repo.
3. Fully rewrite every recipe in the repository during this pass.
4. Convert operations currently handled via recipes into dedicated MCP tools; that is owned by Unity MCP, not this repo.

## Product Positioning

This repo is not a replacement for official Unity MCP.

It is a companion library that helps agents use official Unity MCP better by supplying:

1. Routing rules for when to use a native MCP tool.
2. Curated `Unity_RunCommand` recipes derived from proven working C# implementations.
3. Domain guardrails and high-frequency parameter guidance so the agent does not start from a blank script.

Routing is defined canonically in `## Routing Model` below.

## Source of Truth

### Current repo

- Shared MCP tool matrix: `mcp-tools.md`
- Root routing index: `skills/SKILL.md`
- Topic skills under: `skills/<topic>/SKILL.md`
- Recipe files under: `recipes/*.md`
- Current obsolete sidecar docs:
  - `skills/ui/UI_REFERENCE.md`
  - `skills/uitoolkit/USS_REFERENCE.md`
  - `skills/importer/IMPORT_REFERENCE.md`
  - `skills/probuilder/MODELING_REFERENCE.md`
  - `skills/xr/API_REFERENCE.md`

### Original upstream repo

Use the original repo cloned locally at `/tmp/original-unity-skills` as the authoritative source for recipe repair and command lineage.

Relevant original C# files:

- Asset:
  - `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/AssetSkills.cs`
  - `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/AssetImportSkills.cs`
- UI:
  - `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/UISkills.cs`
- UI Toolkit:
  - `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/UIToolkitSkills.cs`
- Importer-related:
  - `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/AssetImportSkills.cs`
  - `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/TextureSkills.cs`
  - `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/AudioSkills.cs`
  - `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/ModelSkills.cs`
- ProBuilder:
  - `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/ProBuilderSkills.cs`
- XR:
  - `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/XRSkills.cs`
  - `/tmp/original-unity-skills/SkillsForUnity/Editor/Skills/XRReflectionHelper.cs`

Relevant original markdown files:

- Original module index:
  - `/tmp/original-unity-skills/SkillsForUnity/unity-skills~/skills/SKILL.md`
- Original sidecar reference docs:
  - `/tmp/original-unity-skills/SkillsForUnity/unity-skills~/skills/ui/UI_REFERENCE.md`
  - `/tmp/original-unity-skills/SkillsForUnity/unity-skills~/skills/uitoolkit/USS_REFERENCE.md`
  - `/tmp/original-unity-skills/SkillsForUnity/unity-skills~/skills/importer/IMPORT_REFERENCE.md`
  - `/tmp/original-unity-skills/SkillsForUnity/unity-skills~/skills/probuilder/MODELING_REFERENCE.md`
  - `/tmp/original-unity-skills/SkillsForUnity/unity-skills~/skills/xr/API_REFERENCE.md`

## Routing Model

Routing is operation-based, not permanently domain-based.

Rules:

1. Check `mcp-tools.md` first for currently available official Unity MCP tools and their descriptions.
2. If a dedicated official Unity MCP tool covers the requested operation, prefer that tool.
3. If no native tool covers the operation, use the topic skill plus the exact topic recipe as the primary working path.
4. If no exact recipe exists, adapt the closest recipe with minimal changes.
5. If the agent still needs Unity manual or API context, use the related `references/<topic>.md` file as a background-doc fallback.
6. Write a fresh `Unity_RunCommand` script only when neither a native tool nor a close recipe covers the task.

Domains currently using recipes are simply the areas not yet covered by native MCP tools. That set may change over time without requiring a structural rewrite of the repo.

The modules currently handled via recipes in this repo are not obsolete. They are migrated command surfaces derived from the original repo's working C# implementation.

The `references/` folder is a tertiary fallback. It should provide official Unity-doc context after the agent has already checked `mcp-tools.md`, the topic skill, and the topic recipe.

## First-Wave Scope

This cleanup pass focuses first on:

1. `mcp-tools.md`
2. `references/index.md`
3. `skills/SKILL.md`
4. `skills/asset/SKILL.md`
5. `skills/ui/SKILL.md`
6. `skills/uitoolkit/SKILL.md`
7. `skills/importer/SKILL.md`
8. `skills/probuilder/SKILL.md`
9. `skills/xr/SKILL.md`
10. The reference files those skills may point to as tertiary fallbacks:
   - `references/assets.md`
   - `references/ui.md`
   - `references/audio.md`
   - `references/3d.md`
   - `references/xr.md`
11. The topic recipe files those skills should point to:
   - `recipes/asset-recipes.md`
   - `recipes/ui-recipes.md`
   - `recipes/uitoolkit-recipes.md`
   - `recipes/assetimport-recipes.md`
   - `recipes/probuilder-recipes.md`
   - `recipes/xr-recipes.md`
12. Removal of the five obsolete `_REFERENCE.md` files listed above

## Design Decisions

### 1. Remove `_REFERENCE.md` files entirely

The `_REFERENCE.md` files will be deleted rather than renamed.

Reasoning:

1. They encode an old split between "main skill" and "reference sidecar" that does not fit the current repo structure.
2. Their content overlaps with either skill routing guidance or recipe support material.
3. They increase ambiguity about where an agent should look first.

### 2. Move meaningful `_REFERENCE.md` content to one of two destinations

Use this relocation rule:

1. If the content is routing, guardrails, defaults, property warnings, workflow summaries, or domain heuristics, move it into `skills/<topic>/SKILL.md`.
2. If the content is a concrete code workflow, parameter example, or detailed recipe-support material, move it into the exact `recipes/<topic>-recipes.md` file.

Examples:

1. XR verified property names and version caveats belong in `skills/xr/SKILL.md`.
2. UI Toolkit USS-safe workarounds belong in `skills/uitoolkit/SKILL.md` unless they are attached to a concrete recipe example.
3. ProBuilder scale and blockout heuristics belong in `skills/probuilder/SKILL.md`.
4. Concrete workflow snippets originally written as `call_skill(...)` examples should be rewritten for recipe context or reduced to prose guidance.

### 3. Rewrite skill files to use exact recipe references

Replace vague phrases such as:

- "See `recipes/` directory for C# templates."

With exact file references such as:

- "For `Unity_RunCommand` examples, use `recipes/ui-recipes.md`."

Each first-wave topic skill must point to its own recipe file explicitly.

Each affected skill must also reference `mcp-tools.md` as the shared source of truth for native MCP tool availability instead of duplicating a hard-coded list of official tools.

Each affected skill should also point to one or more related `references/*.md` files as tertiary fallbacks when those files are relevant enough to help the agent with Unity-doc context.

### 4. Add an engineered fallback rule for missing recipes

Each skill that is currently handled via recipes should contain equivalent guidance to the following:

> Check `mcp-tools.md` first. If a dedicated official Unity MCP tool covers the operation, use that tool. Otherwise use the exact matching entry in `recipes/<topic>-recipes.md`. If no exact recipe matches, use the closest entry as the starting point and adapt only the minimal required logic. If more Unity manual or API context is still needed, consult `references/<topic>.md`. Write a fresh `Unity_RunCommand` script only when neither a native tool nor a close recipe covers the task.

This replaces weaker wording such as "See recipes" or "use recipes when possible."

### 5. Introduce `references/` as tertiary documentation fallback

`references/` should not compete with recipes. It exists to supply official Unity manual or API context after the agent has already followed the primary execution path.

Rules:

1. `references/index.md` should explain that the usage order is `mcp-tools.md` first, then topic skill plus exact recipe, then topic reference file only if more Unity-doc context is needed.
2. Topic skills should add a `## Related References` section when there is a relevant `references/*.md` file worth consulting.
3. Reference links should be narrow when possible, for example `references/ui.md` for `ui` and `uitoolkit`, or `references/assets.md` for `asset`.
4. Broad or noisy reference files may need light curation before skills point to them.

### 6. Repair recipe files against original source, not against extractor output

Recipe cleanup must not treat the current extracted markdown as authoritative.

Instead:

1. Use the current recipe file to find the intended command inventory.
2. Use the original `.cs` file in `/tmp/original-unity-skills/...` to reconstruct valid `Unity_RunCommand` examples.
3. Remove mechanically broken placeholder lines such as invalid local declarations created by extraction.
4. Keep recipes concise, but ensure they are structurally valid and useful as copy/adapt starting points.

### 7. Introduce `mcp-tools.md` as shared routing truth

`mcp-tools.md` will be maintained as the shared source of truth for currently available official Unity MCP tools and short descriptions of what they cover.

Requirements:

1. It should list the currently available official Unity MCP tools in this environment in a way skills can reference.
2. It should describe tool coverage at the operation level rather than claiming whole domains are permanently native or permanently recipe-backed.
3. Skills should point to `mcp-tools.md` for native-tool routing instead of duplicating a baked-in list.

### 8. Refit updated skill files to `writing-skills` guidance

Every touched `skills/<topic>/SKILL.md` file should be updated to better match the `writing-skills` structure guidance for reusable skills.

Minimum requirements for this cleanup pass:

1. YAML `description` starts with `Use when...` and describes triggering conditions rather than workflow.
2. A clear `## Overview` section remains near the top.
3. A `## When to Use` section is added or clarified.
4. A `## Quick Reference` section is added or the existing command inventory is reshaped to serve that purpose.
5. `## Common Mistakes` stays present and focused on anti-hallucination and misuse.
6. `## Related References` is added when a relevant topic reference file exists.

### 9. Update the root index to remove stale modules and clarify routing

`skills/SKILL.md` must be corrected so it does not link to advisory modules that do not exist in the current repo, such as:

- `project-scout`
- `architecture`
- `adr`
- `asmdef`
- `blueprints`
- `script-roles`
- `scene-contracts`
- `testability`
- `patterns`
- `async`
- `inspector`
- `scriptdesign`

The root index must instead reflect the actual repo contents, reference `mcp-tools.md` for native tool availability, and explain the routing order: native tool first, topic skill plus exact recipe second, related reference file third, fresh `Unity_RunCommand` last.

## Target Structure

### Topic skill structure

For topics currently handled via recipes, `skills/<topic>/SKILL.md` should contain:

1. `## Overview`.
2. `## When to Use`.
3. `## Common Mistakes`.
4. Routing notes that reference `mcp-tools.md` rather than duplicating native tool knowledge.
5. `## Quick Reference` for high-frequency commands and key parameters.
6. Exact recipe file reference.
7. Missing-recipe fallback rule.
8. `## Related References` when a relevant topic reference file exists.

### Topic recipe structure

For first-wave recipe files, each relevant command entry should provide:

1. Command name.
2. Original source signature or a cleaned equivalent.
3. A valid `Unity_RunCommand`-ready example that uses the required `CommandScript` structure.
4. Minimal notes about what the recipe demonstrates and what the agent is expected to adapt.

The recipes do not need to preserve every line of the original method body verbatim if a smaller valid example communicates the same working pattern more clearly.

## Success Criteria

The cleanup pass is successful when all of the following are true:

1. The five `_REFERENCE.md` files are removed.
2. The first-wave skills no longer reference removed `_REFERENCE.md` files.
3. `mcp-tools.md` exists and is positioned as the shared source of truth for currently available official Unity MCP tools and their descriptions.
4. `references/index.md` explains that `references/` is a tertiary documentation fallback after tool and recipe routing.
5. The first-wave skills point to exact recipe files rather than the generic `recipes/` directory.
6. The first-wave skills reference `mcp-tools.md` for native tool routing rather than duplicating brittle tool lists.
7. The first-wave skills are refit to better match `writing-skills` guidance, including `Use when...` descriptions and a clearer skill structure.
8. The root `skills/SKILL.md` no longer links to modules missing from this repo.
9. The first-wave recipe files no longer contain obviously broken extractor placeholders.
10. The repo communicates the routing order: native tool first, topic skill plus exact recipe second, related reference file third, fresh `Unity_RunCommand` last.

## Risks

1. Over-preserving upstream wording may keep REST-era assumptions alive in the rewritten docs.
2. Over-simplifying recipes may remove useful Unity-specific setup details that made the upstream implementation reliable.
3. Attempting to normalize all modules in one pass may expand scope too far and reduce quality.

Mitigation:

1. Treat the original C# as behavioral source of truth, not the original markdown phrasing.
2. Limit the first pass to the defined first-wave modules.
3. Prefer exact recipe references and explicit routing over generic prose.

## Implementation Notes For The Next Planning Step

The implementation plan should include:

1. A file-by-file mapping of which `_REFERENCE.md` content moves into which `SKILL.md` or recipe file.
2. A recipe repair checklist for each first-wave topic, tied back to the original upstream `.cs` files.
3. A plan for creating or updating `mcp-tools.md` from the currently available official Unity MCP tool surface.
4. A plan for updating `references/index.md` and linking vetted `references/*.md` files from the touched skills.
5. Verification steps that grep for removed `_REFERENCE.md` references and generic `See recipes/ directory` phrases after edits.
6. A review pass on README or setup docs only if the rewritten skill-routing language needs corresponding top-level clarification.
