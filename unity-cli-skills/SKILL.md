---
name: unity-cli-skills
description: Use when an agent must inspect, test, or author a Unity project through the standalone Unity CLI and Pipeline without relying on a separate editor-control transport.
---

# Unity CLI Skills

Always target the intended project explicitly and treat its live command catalog as authoritative. Do not
invent command names, parameters, safety semantics, or result shapes from memory. Installing this skill never
authorizes a Unity package or manifest change.

Read [foundation routing and safety](references/domains/foundation.md) before choosing or invoking a route.
Read [profiler capture analysis](references/domains/profiler.md) for explicit profiler sessions, timing and
allocation drill-down, range triage, related-thread evidence, and package-free marker-to-source investigation.
Read the matching canonical reference before editor or project work:

- [GameObjects](references/domains/gameobject.md): exact object identity, hierarchy, transforms, active state, layers, tags, and batched scene mutations.
- [Components](references/domains/component.md): exact component handles, additions/removals, properties, enabled state, and typed-member boundaries.
- [Materials](references/domains/material.md): asset and Renderer-slot targets, shader properties, pipeline-specific emission, texture transforms, and GI flags.
- [ScriptableObjects](references/domains/scriptableobject.md): asset lifecycle, public members, ordered field updates, and validated partial Editor JSON import/export.
- [Prefabs](references/domains/prefab.md): creation, variants, instances, complete override sets, unpacking, and exact persistent property edits.
- [Assets](references/domains/asset.md): folders, copies, moves, search, labels, reimport, recoverable removal, and hash-bound external import.
- [Scenes](references/domains/scene.md): creation, loading, save and Save As, hierarchy/search, active scenes, dirty unload handling, and screenshots.
- [Editor](references/domains/editor.md): state, exact selection, safe menu use, play state, Undo, and Redo.
- [Console](references/domains/console.md): capture boundaries, live entries/export, settings, defines, and reloads.
- [Tests](references/domains/test.md): discovery, clean-scene execution, exact reports, and templates.
- [Project](references/domains/project.md): tags/layers, build/player/quality/rendering settings, packages, and shaders.
- [Packages](references/domains/package.md): installed metadata and guarded lifecycle operations.
- [Scripts](references/domains/script.md): templates, confined search, hash-guarded edits, validation, and cleanup.
- [Tooling](references/domains/tooling.md): persistent Editor tools and their install/compile/menu/uninstall lifecycle.
