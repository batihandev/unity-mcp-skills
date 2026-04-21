# Migration Log
- Phase 1: Repo skeleton created.
- Phase 2: Documentation stripped of Chinese text and converted to standard writing-skills format.
- Phase 3: Core C# utilities (GameObjectFinder/Validate) extracted into pristine templates.
- Phase 4: All 41 REST modules converted to RunCommand recipes.
- Phase 5: REST infrastructure entirely deleted.
**MIGRATION COMPLETE**

## Per-Skill Recipe Split

Ratings:
- `keep` — still valuable as a first-class skill
- `shrink` — keep, but likely trim later because native MCP overlap is high
- `audit` — revisit after the split because current value is unclear

- _shared: rating=keep; shared helper recipes moved to recipes/_shared/*.md.
- animator: rating=keep; recipes split to recipes/animator/<command>.md; skills/animator/SKILL.md refreshed.
- asset: rating=keep; recipes split to recipes/asset/<command>.md; skills/asset/SKILL.md refreshed.
- camera: rating=shrink; recipes split to recipes/camera/<command>.md; skills/camera/SKILL.md refreshed.
- cinemachine: rating=keep; recipes split to recipes/cinemachine/<command>.md; skills/cinemachine/SKILL.md refreshed.
- cleaner: rating=keep; recipes split to recipes/cleaner/<command>.md; skills/cleaner/SKILL.md refreshed.
- component: rating=keep; recipes split to recipes/component/<command>.md; skills/component/SKILL.md refreshed.
- editor: rating=shrink; recipes split to recipes/editor/<command>.md; skills/editor/SKILL.md refreshed.
- event: rating=keep; recipes split to recipes/event/<command>.md; skills/event/SKILL.md refreshed.
- gameobject: rating=keep; recipes split to recipes/gameobject/<command>.md; skills/gameobject/SKILL.md refreshed.
- console: rating=shrink; recipes split to recipes/console/<command>.md; retained debug_force_recompile/debug_get_defines/debug_set_defines merged here.
- debug: rating=remove; standalone debug skill removed; retained define-symbol and recompile commands merged into console.
- importer: rating=keep; recipes split to recipes/importer/<command>.md; skills/importer/SKILL.md refreshed.
- light: rating=keep; recipes split to recipes/light/<command>.md; skills/light/SKILL.md refreshed.
- material: rating=keep; recipes split to recipes/material/<command>.md; skills/material/SKILL.md refreshed.
- navmesh: rating=keep; recipes split to recipes/navmesh/<command>.md; skills/navmesh/SKILL.md refreshed.
- optimization: rating=keep; recipes split to recipes/optimization/<command>.md; skills/optimization/SKILL.md refreshed.
- package: rating=shrink; recipes split to recipes/package/<command>.md; skills/package/SKILL.md refreshed.
- profiler: rating=remove; standalone profiler skill removed; use native Unity_Profiler_* tools through mcp-tools.md.
- perception: rating=keep; recipes split to recipes/perception/<command>.md; skills/perception/SKILL.md refreshed.
- physics: rating=keep; recipes split to recipes/physics/<command>.md; skills/physics/SKILL.md refreshed.
- prefab: rating=keep; recipes split to recipes/prefab/<command>.md; skills/prefab/SKILL.md refreshed.
- probuilder: rating=keep; recipes split to recipes/probuilder/<command>.md; skills/probuilder/SKILL.md refreshed.
- project: rating=shrink; recipes split to recipes/project/<command>.md; skills/project/SKILL.md refreshed; native Unity_GetProjectData overlap documented.
- sample: rating=keep; recipes split to recipes/sample/<command>.md; skills/sample/SKILL.md refreshed.
- scene: rating=keep; recipes split to recipes/scene/<command>.md; skills/scene/SKILL.md refreshed.
- script: rating=shrink; kept as guardrails only; API tables removed; no standalone recipe split retained.
- scriptableobject: rating=keep; recipes split to recipes/scriptableobject/<command>.md; skills/scriptableobject/SKILL.md refreshed.
- shader: rating=keep; recipes split to recipes/shader/<command>.md; skills/shader/SKILL.md refreshed.
- smart: rating=keep; recipes split to recipes/smart/<command>.md; skills/smart/SKILL.md refreshed.
- terrain: rating=keep; recipes split to recipes/terrain/<command>.md; skills/terrain/SKILL.md refreshed.
- test: rating=keep; recipes split to recipes/test/<command>.md; skills/test/SKILL.md refreshed.
- timeline: rating=keep; recipes split to recipes/timeline/<command>.md; skills/timeline/SKILL.md refreshed.
- ui: rating=keep; recipes split to recipes/ui/<command>.md; skills/ui/SKILL.md refreshed.
- uitoolkit: rating=keep; recipes split to recipes/uitoolkit/<command>.md; skills/uitoolkit/SKILL.md refreshed.
- validation: rating=keep; recipes split to recipes/validation/<command>.md; skills/validation/SKILL.md refreshed.
- workflow: rating=keep; recipes split to recipes/workflow/<command>.md; skills/workflow/SKILL.md refreshed.
- xr: rating=keep; recipes split to recipes/xr/<command>.md; skills/xr/SKILL.md refreshed.
