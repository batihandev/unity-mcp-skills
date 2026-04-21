# Project Recipes

Per-command recipes for the `unity-project` skill. Each file corresponds to one command ID.

## Commands

| File | Command |
|------|---------|
| [project_get_info.md](project_get_info.md) | `project_get_info` |
| [project_get_render_pipeline.md](project_get_render_pipeline.md) | `project_get_render_pipeline` |
| [project_list_shaders.md](project_list_shaders.md) | `project_list_shaders` |
| [project_get_quality_settings.md](project_get_quality_settings.md) | `project_get_quality_settings` |
| [project_get_build_settings.md](project_get_build_settings.md) | `project_get_build_settings` |
| [project_get_packages.md](project_get_packages.md) | `project_get_packages` |
| [project_get_layers.md](project_get_layers.md) | `project_get_layers` |
| [project_get_tags.md](project_get_tags.md) | `project_get_tags` |
| [project_add_tag.md](project_add_tag.md) | `project_add_tag` |
| [project_get_player_settings.md](project_get_player_settings.md) | `project_get_player_settings` |
| [project_set_quality_level.md](project_set_quality_level.md) | `project_set_quality_level` |

## Native Tool Overlap

`project_get_info` overlaps with the native `Unity_GetProjectData` tool. Prefer `Unity_GetProjectData` for a quick summary of project metadata; use `project_get_info` when you also need render pipeline details or pipeline-aware shader recommendations.

## Usage

Use these templates in `Unity_RunCommand`. Recipe path rule: `../../recipes/project/<command>.md`
