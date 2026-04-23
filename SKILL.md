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
3. **Closest recipe** — adapt the nearest recipe if no exact match exists.
4. **`references/index.md`** — lists all available topics; consult `references/<topic>.md` as a tertiary fallback.
5. **Fresh `Unity_RunCommand`** — last resort.

## Domain Skill Map

| Domain | Skill file | Tool route |
|--------|------------|------------|
| gameobject | `skills/gameobject/SKILL.md` | `Unity_RunCommand` |
| component | `skills/component/SKILL.md` | `Unity_RunCommand` |
| material | `skills/material/SKILL.md` | `Unity_RunCommand` |
| light | `skills/light/SKILL.md` | `Unity_RunCommand` |
| prefab | `skills/prefab/SKILL.md` | `Unity_RunCommand` |
| asset | `skills/asset/SKILL.md` | `Unity_RunCommand` |
| ui | `skills/ui/SKILL.md` | `Unity_RunCommand` |
| uitoolkit | `skills/uitoolkit/SKILL.md` | `Unity_RunCommand` |
| script | `skills/script/SKILL.md` | Native MCP only (`Unity_CreateScript` / `Unity_ScriptApplyEdits` / `Unity_ValidateScript` / …) — no recipes |
| scene | `skills/scene/SKILL.md` | `Unity_RunCommand` |
| editor | `skills/editor/SKILL.md` | `Unity_RunCommand` |
| animator | `skills/animator/SKILL.md` | `Unity_RunCommand` |
| shader | `skills/shader/SKILL.md` | `Unity_RunCommand` |
| console | `skills/console/SKILL.md` | `Unity_ReadConsole` |
| validation | `skills/validation/SKILL.md` | `Unity_RunCommand` |
| importer | `skills/importer/SKILL.md` | `Unity_RunCommand` |
| cinemachine | `skills/cinemachine/SKILL.md` | `Unity_RunCommand` |
| probuilder | `skills/probuilder/SKILL.md` | `Unity_RunCommand` |
| xr | `skills/xr/SKILL.md` | `Unity_RunCommand` |
| terrain | `skills/terrain/SKILL.md` | `Unity_RunCommand` |
| physics | `skills/physics/SKILL.md` | `Unity_RunCommand` |
| navmesh | `skills/navmesh/SKILL.md` | `Unity_RunCommand` |
| timeline | `skills/timeline/SKILL.md` | `Unity_RunCommand` |
| cleaner | `skills/cleaner/SKILL.md` | `Unity_RunCommand` |
| smart | `skills/smart/SKILL.md` | `Unity_RunCommand` |
| perception | `skills/perception/SKILL.md` | `Unity_RunCommand` |
| camera | `skills/camera/SKILL.md` | `Unity_Camera_Capture` |
| event | `skills/event/SKILL.md` | `Unity_RunCommand` |
| package | `skills/package/SKILL.md` | Native MCP only (`Unity_PackageManager_ExecuteAction` / `Unity_PackageManager_GetData`) |
| project | `skills/project/SKILL.md` | `Unity_GetProjectData` |
| optimization | `skills/optimization/SKILL.md` | `Unity_RunCommand` |
| test | `skills/test/SKILL.md` | `Unity_RunCommand` |
| scriptableobject | `skills/scriptableobject/SKILL.md` | `Unity_RunCommand` |

All paths above are relative to the library root (this file's directory).

## Other root files

- `skills/SKILL.md` — internal index of the same domain map (for cross-linking from domain skills).
- `mcp-tools.md` — full routing matrix for native MCP tools vs `Unity_RunCommand`.
- `recipes/README.md` — recipes intro and conventions.
- `recipes/_shared/README.md` — cross-domain C# helpers (`gameobject_finder`, `skills_common`, `validate`) embedded by many recipes.
- `references/index.md` — catalog of all `references/<topic>.md` fallback docs.
