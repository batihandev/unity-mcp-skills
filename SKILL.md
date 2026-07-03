---
name: unity-mcp-skills
description: Use when working with a Unity project via MCP — creating or editing scenes, GameObjects, components, prefabs, materials, UI, lighting, navmesh, animations, terrain, C# scripts, or any other Unity Editor task.
---

# Unity MCP Skills Library

## Overview

The single entry point for Unity work via MCP. Domain skills under `skills/<domain>/SKILL.md` are loaded on demand from this router — read this file first, then navigate to the domain file that matches the task.

## Integrity Check

If during a task you find a referenced file is missing or a path does not resolve
(e.g. `../../recipes/scene/scene_create.md` not found, a `recipes/_shared/` helper
missing, `../../mcp-tools.md` missing, or a `references/<topic>.md` fallback missing),
stop and tell the user:

> The Unity skills library appears to be incomplete or out of date. Please reinstall
> or update the `unity-mcp-skills` folder in your agent's skills directory to the latest
> version from https://github.com/batihandev/unity-mcp-skills, then retry.

Do not attempt to work around missing files by fabricating recipe content.

## Routing Order

1. **Native MCP tool** — see `mcp-tools.md` (library root) for the routing matrix.
2. **Topic skill + exact recipe** — load the matching domain skill below, then its recipe in `recipes/<domain>/`.
3. **Install-once Editor tool** — if the task needs a persistent Editor script, check `tooling/<domain>/` via `skills/tooling/SKILL.md`.
4. **Closest recipe** — adapt the nearest recipe if no exact match exists.
5. **`references/index.md`** — lists all available topics; consult `references/<topic>.md` as a tertiary fallback.
6. **Fresh `Unity_RunCommand`** — last resort; allowed only under the RunCommand Contract below.

## RunCommand Contract

Before the first `Unity_RunCommand` in a domain, read that domain's `skills/<domain>/SKILL.md` and the recipe it routes to. Every `Unity_RunCommand` body either adapts a named recipe (`recipes/<domain>/<command>.md` — name it) or states that no recipe covers the case. Do not send a fresh body without saying which of the two applies.

## Execution trees

Two trees, route by execution model first:

- `recipes/<domain>/` — `Unity_RunCommand` one-shot bodies, compile/run-gated.
- `tooling/<domain>/` — `Unity_CreateScript` install-once Editor scripts, no gate. See [`tooling/README.md`](tooling/README.md).

The Domain Skill Map below indexes `recipes/`. For install-once tools, open `tooling/<domain>/README.md` under the same domain name.

## Domain Skill Map

| Domain | What it covers | Skill file | Tool route |
|--------|----------------|------------|------------|
| gameobject | Object create/move/parent | `skills/gameobject/SKILL.md` | `Unity_RunCommand` |
| component | Component add/remove/configure | `skills/component/SKILL.md` | `Unity_RunCommand` |
| material | Material property edits | `skills/material/SKILL.md` | `Unity_RunCommand` |
| light | Light create/configure | `skills/light/SKILL.md` | `Unity_RunCommand` |
| prefab | Prefab create/apply/spawn | `skills/prefab/SKILL.md` | `Unity_RunCommand` |
| asset | Asset refresh/find/info | `skills/asset/SKILL.md` | `Unity_RunCommand` |
| ui | UGUI Canvas/UI creation | `skills/ui/SKILL.md` | `Unity_RunCommand` |
| uitoolkit | UXML/USS/UIDocument | `skills/uitoolkit/SKILL.md` | `Unity_RunCommand` |
| script | Script edit/validate/SHA | `skills/script/SKILL.md` | Native MCP only (`Unity_CreateScript` / `Unity_ScriptApplyEdits` / `Unity_ValidateScript` / …) — no recipes |
| tooling | Install-once Editor script templates | `skills/tooling/SKILL.md` | `Unity_CreateScript` (templates under `tooling/<domain>/`) |
| scene | Scene load/save/query | `skills/scene/SKILL.md` | `Unity_RunCommand` |
| editor | Play/select/undo/redo | `skills/editor/SKILL.md` | `Unity_RunCommand` |
| animator | Animator controllers | `skills/animator/SKILL.md` | `Unity_RunCommand` |
| shader | Shader create/list | `skills/shader/SKILL.md` | `Unity_RunCommand` |
| console | Log capture/debug | `skills/console/SKILL.md` | `Unity_ReadConsole` |
| validation | Broken reference checks | `skills/validation/SKILL.md` | `Unity_RunCommand` |
| importer | Texture/audio/model import | `skills/importer/SKILL.md` | `Unity_RunCommand` |
| cinemachine | VCam operations | `skills/cinemachine/SKILL.md` | `Unity_RunCommand` |
| probuilder | ProBuilder mesh edits | `skills/probuilder/SKILL.md` | `Unity_RunCommand` |
| xr | XRI setup | `skills/xr/SKILL.md` | `Unity_RunCommand` |
| terrain | Terrain create/paint | `skills/terrain/SKILL.md` | `Unity_RunCommand` |
| physics | Raycast/overlap/gravity | `skills/physics/SKILL.md` | `Unity_RunCommand` |
| navmesh | NavMesh bake/query | `skills/navmesh/SKILL.md` | `Unity_RunCommand` |
| timeline | Timeline tracks/clips | `skills/timeline/SKILL.md` | `Unity_RunCommand` |
| cleaner | Unused/duplicate assets | `skills/cleaner/SKILL.md` | `Unity_RunCommand` |
| smart | Query/layout/auto-bind | `skills/smart/SKILL.md` | `Unity_RunCommand` |
| perception | Scene/project analysis | `skills/perception/SKILL.md` | `Unity_RunCommand` |
| camera | Scene View camera | `skills/camera/SKILL.md` | `Unity_Camera_Capture` (Scene View / in-memory); game-camera PNG at a path/resolution → `camera_screenshot` recipe — do not hand-roll a RenderTexture |
| event | UnityEvent wiring | `skills/event/SKILL.md` | `Unity_RunCommand` |
| package | UPM install/query | `skills/package/SKILL.md` | Native MCP only (`Unity_PackageManager_ExecuteAction` / `Unity_PackageManager_GetData`) |
| project | Project info/settings | `skills/project/SKILL.md` | `Unity_GetProjectData` |
| optimization | Asset optimization | `skills/optimization/SKILL.md` | `Unity_RunCommand` |
| test | Unity Test Runner | `skills/test/SKILL.md` | `Unity_RunCommand` |
| scriptableobject | ScriptableObject assets | `skills/scriptableobject/SKILL.md` | `Unity_RunCommand` |

All paths above are relative to the library root (this file's directory).

## Other root files

- `mcp-tools.md` — full routing matrix for native MCP tools vs `Unity_RunCommand`.
- `recipes/README.md` — recipes intro and conventions.
- `tooling/README.md` — install-once Editor script templates (not subject to RunCommand validation); domain subfolders mirror `recipes/`.
- `recipes/_shared/README.md` — cross-domain C# helpers embedded by many recipes: `execution_result`, `validate`, `workflow_manager`, `gameobject_finder`, `skills_common`, `component_type_finder`, `value_converter`, `project_skills`, `perception_helpers`.
- `references/index.md` — catalog of all `references/<topic>.md` fallback docs.
