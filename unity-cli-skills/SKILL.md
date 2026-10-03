---
name: unity-cli-skills
description: Use when an agent must inspect, test, or author a Unity project through the standalone Unity CLI and Pipeline without relying on a separate editor-control transport.
---

# Unity CLI Skills

Always target the intended project explicitly and treat its live command catalog as authoritative. Do not
invent command names, parameters, safety semantics, or result shapes from memory. Installing this skill never
authorizes a Unity package or manifest change.

The host workflow helpers require Python 3.10 or newer and no optional Python packages. Discover the CLI and
Editor versions for the selected host, or pass explicit executable paths and version values when discovery is
ambiguous. The verified route is a graphics-enabled Windows Editor controlled from a WSL/Linux shell.
Native Windows, Linux, and macOS routes are expected-unverified. Check the repository compatibility
matrix for exact versions and do not assume other cross-OS combinations.

Read [foundation routing and safety](references/domains/foundation.md) before choosing or invoking a route.
For session discovery, opening, readiness, closing, restart, or recovery, use the [session lifecycle guide](references/domains/session.md)
and its sole owner, `python scripts/unity_session.py ACTION --project PATH`.
For compile, connected or offline tests, and console capture, use the documented commands in
`python scripts/unity_workflow.py` and their respective [editor](references/domains/editor.md),
[test](references/domains/test.md), and [console](references/domains/console.md) guides. Do not duplicate
host process, identity, or session recovery logic in a domain command.
For ephemeral C# probes and compile-only checks, use [probe execution](references/domains/probe.md)
and `python scripts/unity_probe.py`. Require current-sequence completion and preserve its raw report.
Read [profiler capture analysis](references/domains/profiler.md) for explicit profiler sessions, timing and
allocation drill-down, range triage, related-thread evidence, and package-free marker-to-source investigation.
Read the matching canonical reference before editor or project work:

- [GameObjects](references/domains/gameobject.md): exact object identity, hierarchy, transforms, active state, layers, tags, and batched scene mutations.
- [Components](references/domains/component.md): exact component handles, additions/removals, properties, enabled state, and typed-member boundaries.
- [Timeline](references/domains/timeline.md): native Timeline asset/tracks/sourced clips and optional typed bindings, default clips, duration, removal, and Director transport.
- [Cinemachine](references/domains/cinemachine.md): exact cameras, pipeline stages, serialized controls, managers, groups, blends, splines, and impulses.
- [ProBuilder](references/domains/probuilder.md): editable shapes, exact face/edge/vertex edits, UVs, materials, pivots, and combined meshes.
- [Animator](references/domains/animator.md): native controller asset authoring, exact Animator assignment, runtime parameters and playback, and bounded read-only state inspection.
- [Canvas and uGUI](references/domains/ui.md): optional typed widget factories, layout, selection alignment/distribution, bounded UI queries and native component composition.
- [UI Toolkit](references/domains/uitoolkit.md): UXML/USS content, ten starters, generated query scripts, UIDocument and persistent PanelSettings composition.
- [XR](references/domains/xr.md): optional typed rig, interactor/interactable, locomotion, tracked UI, haptics, XRI layers and persistent interaction-event authoring.
- [UnityEvents](references/domains/event.md): exact persistent listener inspection, authoring, state changes, copying, and confirmed invocation.
- [Physics](references/domains/physics.md): bounded casts and overlaps, gravity, PhysicsMaterial assets and collider assignment, and guarded layer collision settings.
- [Terrain](references/domains/terrain.md): exact Terrain creation, inspection, sampled heights, heightmap editing, and existing-layer painting.
- [NavMesh](references/domains/navmesh.md): per-surface AI Navigation builds and removal, agent and obstacle components, path queries, and guarded area costs.
- [Cleaner](references/domains/cleaner.md): scoped duplicate, size, dependency, folder and missing-reference reports; recoverable Trash and scene missing-script Undo.
- [Optimization](references/domains/optimization.md): rendering and material heuristics, guarded importer plans, static flags and LOD setup.
- [Perception](references/domains/perception.md): scene context, metrics, serialized references, script dependencies, project structure and report export.
- [Validation](references/domains/validation.md): missing scripts and references, asset candidates, texture and shader diagnostics, colliders and recoverable cleanup.
- [Smart](references/domains/smart.md): grouped transform tools, physics and property queries, exact selection, reference binding and prefab replacement.
- [Importer and media](references/domains/importer.md): guarded audio, texture, and model importer capture, preflight, writes, readback, restoration, media inspection, and nontransactional batches.
- [Lighting and cameras](references/domains/lighting-camera.md): native Light/Camera/probe authoring, Scene View framing, exact-camera screenshots, and measured observer framing.
- [Materials](references/domains/material.md): asset and Renderer-slot targets, shader properties, pipeline-specific emission, texture transforms, and GI flags.
- [Shaders](references/domains/shader.md): shader asset creation, exact inspection, source reads, recoverable removal, and global keywords.
- [ScriptableObjects](references/domains/scriptableobject.md): asset lifecycle, public members, ordered field updates, and validated partial Editor JSON import/export.
- [Prefabs](references/domains/prefab.md): creation, variants, instances, complete override sets, unpacking, and exact persistent property edits.
- [Assets](references/domains/asset.md): folders, copies, moves, search, labels, reimport, recoverable removal, and hash-bound external import.
- [Scenes](references/domains/scene.md): creation, loading, save and Save As, hierarchy/search, active scenes, Scene View state, dirty unload handling, and screenshots.
- [Editor](references/domains/editor.md): state, exact selection, safe menu use, play state, Undo, and Redo.
- [Console](references/domains/console.md): capture boundaries, live entries/export, settings, defines, and reloads.
- [Tests](references/domains/test.md): discovery, clean-scene execution, exact reports, and templates.
- [Project](references/domains/project.md): tags/layers, build/player/quality/rendering settings, packages, and shaders.
- [Packages](references/domains/package.md): installed metadata and guarded lifecycle operations.
- [Scripts](references/domains/script.md): templates, confined search, hash-guarded edits, validation, and cleanup.
- [Tooling](references/domains/tooling.md): persistent Editor tools and their install/compile/menu/uninstall lifecycle.
