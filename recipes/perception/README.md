# Perception Recipes

C# `RunCommand` templates for the `unity-perception` skill module. Each file covers one command.

## Commands

| Command | File | Description |
|---------|------|-------------|
| `scene_analyze` | [scene_analyze.md](scene_analyze.md) | Full combined diagnosis: health + contract + stack + component stats |
| `scene_health_check` | [scene_health_check.md](scene_health_check.md) | Read-only hygiene report (missing scripts, refs, facilities, hotspots) |
| `scene_summarize` | [scene_summarize.md](scene_summarize.md) | Fast single-pass scene overview with object counts and top components |
| `scene_component_stats` | [scene_component_stats.md](scene_component_stats.md) | Component and facility statistics |
| `scene_find_hotspots` | [scene_find_hotspots.md](scene_find_hotspots.md) | Deep hierarchy / large child group / empty node hotspots |
| `scene_contract_validate` | [scene_contract_validate.md](scene_contract_validate.md) | Validate root names, tags, layers, and UI conventions |
| `scene_tag_layer_stats` | [scene_tag_layer_stats.md](scene_tag_layer_stats.md) | Tag and layer usage across all scene objects |
| `scene_performance_hints` | [scene_performance_hints.md](scene_performance_hints.md) | Prioritized optimization hints (shadows, batching, LOD, materials, particles) |
| `scene_diff` | [scene_diff.md](scene_diff.md) | Capture or compare lightweight scene snapshots |
| `hierarchy_describe` | [hierarchy_describe.md](hierarchy_describe.md) | Human-readable text hierarchy tree |
| `scene_context` | [scene_context.md](scene_context.md) | Structured hierarchy + components + references export for AI coding context |
| `scene_export_report` | [scene_export_report.md](scene_export_report.md) | Save a full markdown scene report to disk |
| `scene_dependency_analyze` | [scene_dependency_analyze.md](scene_dependency_analyze.md) | Analyze serialized reference impact / dependency graph in-scene |
| `scene_materials` | [scene_materials.md](scene_materials.md) | Summarize scene materials and shaders grouped by shader |
| `scene_spatial_query` | [scene_spatial_query.md](scene_spatial_query.md) | Find objects near a point or another object |
| `project_stack_detect` | [project_stack_detect.md](project_stack_detect.md) | Detect render pipeline, input system, UI route, and installed packages |
| `script_analyze` | [script_analyze.md](script_analyze.md) | Analyze one MonoBehaviour / ScriptableObject / user class by name |
| `script_dependency_graph` | [script_dependency_graph.md](script_dependency_graph.md) | N-hop bidirectional dependency closure for one script |

## Choosing the Right Command

| Need | Best first command |
|------|--------------------|
| Quick overview | `scene_summarize` |
| Full diagnosis | `scene_analyze` |
| Hygiene / red flags only | `scene_health_check` |
| Clutter or deep hierarchy | `scene_find_hotspots` |
| Convention compliance | `scene_contract_validate` |
| Impact of deleting an object | `scene_dependency_analyze` |
| AI coding context export | `scene_context` |
| Script reading order | `script_dependency_graph` |
| Render/input/UI stack | `project_stack_detect` |
| Before/after change verification | `scene_diff` |
