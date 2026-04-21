# Recipe Validation Tracker

State as of 2026-04-21. Updated in-place by `tools/tracker_update.py`; queried by `tools/tracker_next.py`.

## Gates

- **ext** — body re-extracted from upstream (or never stubbed).
- **pre** — `## Prerequisites` section declares every `_shared/*.md` it uses.
- **comp** — passes compile-only Unity_RunCommand (body in `if (false)`).
- **run** — passes end-to-end Unity_RunCommand.

Cell values: `x` = done, `-` = pending, `B` = blocker (see notes), `R` = retired (all gates share this value; notes column records the redirect target — MCP tool name or replacement recipe path).

## Summary

- Total recipes: **484**
- ext: **484** / 484
- pre: **484** / 484
- comp: **19** / 484
- run: **1** / 484
- retired: **0** / 484

## Domains

[animator](#animator-10-recipes) · [asset](#asset-16-recipes) · [camera](#camera-11-recipes) · [cinemachine](#cinemachine-34-recipes) · [cleaner](#cleaner-10-recipes) · [component](#component-10-recipes) · [console](#console-13-recipes) · [editor](#editor-12-recipes) · [event](#event-10-recipes) · [gameobject](#gameobject-18-recipes) · [importer](#importer-39-recipes) · [light](#light-10-recipes) · [material](#material-21-recipes) · [navmesh](#navmesh-10-recipes) · [optimization](#optimization-10-recipes) · [package](#package-11-recipes) · [perception](#perception-18-recipes) · [physics](#physics-12-recipes) · [prefab](#prefab-11-recipes) · [probuilder](#probuilder-22-recipes) · [project](#project-11-recipes) · [sample](#sample-8-recipes) · [scene](#scene-10-recipes) · [scriptableobject](#scriptableobject-10-recipes) · [shader](#shader-11-recipes) · [smart](#smart-10-recipes) · [terrain](#terrain-10-recipes) · [test](#test-11-recipes) · [timeline](#timeline-12-recipes) · [ui](#ui-26-recipes) · [uitoolkit](#uitoolkit-25-recipes) · [validation](#validation-10-recipes) · [xr](#xr-22-recipes)

## animator (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| animator_add_parameter | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| animator_add_state | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| animator_add_transition | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| animator_assign_controller | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| animator_create_controller | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| animator_get_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| animator_get_parameters | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| animator_list_states | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| animator_play | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| animator_set_parameter | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## asset (16 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| asset_create_folder | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| asset_delete | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| asset_delete_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| asset_duplicate | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| asset_find | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| asset_get_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| asset_get_labels | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| asset_import | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| asset_import_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| asset_move | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| asset_move_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| asset_refresh | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| asset_reimport | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: normalize summary |
| asset_reimport_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: scripted via inject_prerequisites.py |
| asset_set_labels | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| batch_query_assets | x | x | B | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: recipe body is top-level REPL script, not CommandScript; uses Newtonsoft.Json which is unavailable in Unity_RunCommand |

## camera (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| camera_align_view_to_object | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| camera_create | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| camera_get_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| camera_get_properties | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| camera_list | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| camera_look_at | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| camera_screenshot | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| camera_set_culling_mask | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| camera_set_orthographic | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| camera_set_properties | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| camera_set_transform | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## cinemachine (34 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| cinemachine_add_component | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_add_extension | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_configure_aim | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_configure_body | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_configure_camera_manager | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_configure_extension | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_configure_impulse_source | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_create_clear_shot | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_create_freelook | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_create_mixing_camera | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_create_sequencer | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_create_state_driven_camera | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_create_target_group | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_create_vcam | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_get_brain_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_impulse_generate | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_inspect_vcam | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_list_components | x | x | B | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses CinemachineAdapter.CmAssembly — upstream helper not ported |
| cinemachine_mixing_camera_set_weight | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_remove_extension | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_sequencer_add_instruction | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_set_active | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_set_blend | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_set_brain | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_set_component | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_set_lens | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_set_noise | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_set_priority | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_set_spline | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_set_targets | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_set_vcam_property | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_state_driven_camera_add_instruction | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_target_group_add_member | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cinemachine_target_group_remove_member | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## cleaner (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| cleaner_delete_assets | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cleaner_delete_empty_folders | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cleaner_find_duplicates | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cleaner_find_empty_folders | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cleaner_find_large_assets | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cleaner_find_missing_references | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cleaner_find_unused_assets | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cleaner_fix_missing_scripts | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cleaner_get_asset_usage | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| cleaner_get_dependency_tree | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## component (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| component_add | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| component_add_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| component_copy | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| component_get_properties | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| component_list | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| component_remove | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| component_remove_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| component_set_enabled | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| component_set_property | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| component_set_property_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |

## console (13 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| console_clear | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| console_export | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| console_get_logs | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| console_get_stats | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| console_log | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| console_set_clear_on_play | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| console_set_collapse | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| console_set_pause_on_error | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| console_start_capture | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| console_stop_capture | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| debug_force_recompile | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| debug_get_defines | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| debug_set_defines | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## editor (12 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| editor_execute_menu | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| editor_get_context | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| editor_get_layers | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| editor_get_selection | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| editor_get_state | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| editor_get_tags | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| editor_pause | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| editor_play | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| editor_redo | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| editor_select | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| editor_stop | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| editor_undo | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## event (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| event_add_listener | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| event_add_listener_batch | x | x | B | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses Newtonsoft JsonConvert + undefined SkillResultHelper; body contains NotImplementedException |
| event_clear_listeners | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| event_copy_listeners | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| event_get_listener_count | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| event_get_listeners | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| event_invoke | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| event_list_events | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| event_remove_listener | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| event_set_listener_state | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## gameobject (18 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| gameobject_create | x | x | x | x | 2026-04-21: pilot; 2026-04-21: pilot; 2026-04-21: pilot; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_create_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_delete | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_delete_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_duplicate | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_duplicate_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_find | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_get_info | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_rename | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_rename_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_set_active | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_set_active_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_set_layer_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_set_parent | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_set_parent_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_set_tag_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_set_transform | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| gameobject_set_transform_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |

## importer (39 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| asset_reimport | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| asset_reimport_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| audio_add_source | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| audio_create_mixer | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| audio_find_clips | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| audio_find_sources_in_scene | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| audio_get_clip_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| audio_get_import_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| audio_get_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| audio_get_source_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| audio_set_import_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| audio_set_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| audio_set_settings_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| audio_set_source_properties | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| model_find_assets | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| model_get_animations_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| model_get_import_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| model_get_materials_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| model_get_mesh_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| model_get_rig_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| model_get_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| model_set_animation_clips | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| model_set_import_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| model_set_rig | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| model_set_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| model_set_settings_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| sprite_set_import_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| texture_find_assets | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| texture_find_by_size | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| texture_get_import_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| texture_get_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| texture_get_platform_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| texture_get_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| texture_set_import_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| texture_set_platform_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| texture_set_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| texture_set_settings_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| texture_set_sprite_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| texture_set_type | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## light (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| light_add_probe_group | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| light_add_reflection_probe | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| light_create | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| light_find_all | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| light_get_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| light_get_lightmap_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| light_set_enabled | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| light_set_enabled_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| light_set_properties | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| light_set_properties_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## material (21 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| material_assign | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_assign_batch | x | x | B | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses BatchExecutor + SkillResultHelper not in _shared |
| material_create | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_create_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_duplicate | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_get_keywords | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_get_properties | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_color | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_colors_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_emission | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_emission_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_float | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_gi_flags | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_int | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_keyword | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_render_queue | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_shader | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_texture | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_texture_offset | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_texture_scale | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| material_set_vector | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |

## navmesh (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| navmesh_add_agent | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| navmesh_add_obstacle | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| navmesh_bake | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| navmesh_calculate_path | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| navmesh_clear | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| navmesh_get_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| navmesh_sample_position | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| navmesh_set_agent | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| navmesh_set_area_cost | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| navmesh_set_obstacle | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## optimization (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| optimize_analyze_overdraw | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| optimize_analyze_scene | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| optimize_audio_compression | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| optimize_find_duplicate_materials | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| optimize_find_large_assets | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| optimize_get_static_flags | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| optimize_mesh_compression | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| optimize_set_lod_group | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| optimize_set_static_flags | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| optimize_textures | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## package (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| package_check | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| package_get_cinemachine_status | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| package_get_dependencies | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| package_get_versions | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| package_install | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| package_install_cinemachine | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| package_install_splines | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| package_list | x | x | B | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses PackageManagerHelper.InstalledPackages — upstream helper not ported |
| package_refresh | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| package_remove | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| package_search | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## perception (18 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| hierarchy_describe | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| project_stack_detect | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_analyze | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_component_stats | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_context | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_contract_validate | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_dependency_analyze | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_diff | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_export_report | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_find_hotspots | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_health_check | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_materials | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_performance_hints | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_spatial_query | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_summarize | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_tag_layer_stats | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| script_analyze | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| script_dependency_graph | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## physics (12 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| physics_boxcast | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| physics_check_overlap | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| physics_create_material | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| physics_get_gravity | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| physics_get_layer_collision | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| physics_overlap_box | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| physics_raycast | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| physics_raycast_all | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| physics_set_gravity | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| physics_set_layer_collision | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| physics_set_material | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| physics_spherecast | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## prefab (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| prefab_apply | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| prefab_apply_overrides | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| prefab_create | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| prefab_create_variant | x | x | B | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: body references undeclared sourcePrefabPath/variantPath parameters; missing using System.IO |
| prefab_find_instances | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| prefab_get_overrides | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| prefab_instantiate | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| prefab_instantiate_batch | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| prefab_revert_overrides | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| prefab_set_property | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| prefab_unpack | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |

## probuilder (22 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| probuilder_bevel_edges | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_bridge_edges | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_center_pivot | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_combine_meshes | x | x | B | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: bridge-stub extract carries upstream issues: local 'result' var shadows ExecutionResult param; uses #if !PROBUILDER + upstream helpers (NoProBuilder, CombineMeshes.Combine) not in _shared |
| probuilder_conform_normals | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_create_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_create_shape | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_delete_faces | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_detach_faces | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_extrude_edges | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_extrude_faces | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_flip_normals | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_get_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_get_vertices | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_merge_faces | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_move_vertices | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_project_uv | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_set_face_material | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_set_material | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_set_vertices | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_subdivide | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| probuilder_weld_vertices | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## project (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| project_add_tag | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| project_get_build_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| project_get_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| project_get_layers | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| project_get_packages | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| project_get_player_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| project_get_quality_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| project_get_render_pipeline | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| project_get_tags | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| project_list_shaders | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| project_set_quality_level | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## sample (8 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| create_cube | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| create_sphere | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| delete_object | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| find_objects_by_name | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| get_scene_info | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| set_object_position | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| set_object_rotation | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| set_object_scale | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |

## scene (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| scene_create | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_find_objects | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_get_hierarchy | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_get_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_get_loaded | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_load | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| scene_save | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_screenshot | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_set_active | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scene_unload | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## scriptableobject (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| scriptableobject_create | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scriptableobject_delete | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scriptableobject_duplicate | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scriptableobject_export_json | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scriptableobject_find | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| scriptableobject_get | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scriptableobject_import_json | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scriptableobject_list_types | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scriptableobject_set | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| scriptableobject_set_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## shader (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| shader_check_errors | x | x | B | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses FindShaderByNameOrPath — private upstream helper not in _shared |
| shader_create | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| shader_create_urp | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| shader_delete | x | x | B | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: Unity MCP returns 'User interactions not supported' on repeat attempts; compile-only appears blocked at this recipe; deferred |
| shader_find | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| shader_get_keywords | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| shader_get_properties | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| shader_get_variant_count | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| shader_list | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| shader_read | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| shader_set_global_keyword | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |

## smart (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| smart_align_to_ground | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| smart_distribute | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| smart_randomize_transform | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| smart_reference_bind | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| smart_replace_objects | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| smart_scene_layout | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| smart_scene_query | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| smart_scene_query_spatial | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| smart_select_by_component | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| smart_snap_to_grid | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## terrain (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| terrain_add_hill | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| terrain_create | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| terrain_flatten | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| terrain_generate_perlin | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| terrain_get_height | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| terrain_get_info | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| terrain_paint_texture | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| terrain_set_height | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| terrain_set_heights_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| terrain_smooth | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## test (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| test_cancel | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| test_create_editmode | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| test_create_playmode | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| test_get_last_result | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| test_get_result | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| test_get_summary | x | x | B | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses undefined private helpers EnumerateRealTestRuns/GetResultInt/GetResultStringList from upstream TestSkills |
| test_list | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| test_list_categories | x | x | B | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses undefined DiscoverTests helper from upstream TestSkills |
| test_run | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| test_run_by_name | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| test_smoke_skills | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## timeline (12 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| timeline_add_activation_track | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| timeline_add_animation_track | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| timeline_add_audio_track | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| timeline_add_clip | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| timeline_add_control_track | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| timeline_add_signal_track | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| timeline_create | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| timeline_list_tracks | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| timeline_play | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| timeline_remove_track | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| timeline_set_binding | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| timeline_set_duration | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## ui (26 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| ui_add_canvas_group | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_add_layout_element | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_add_mask | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_add_outline | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_align_selected | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_configure_selectable | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_create_batch | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_create_button | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_create_canvas | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_create_dropdown | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_create_image | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_create_inputfield | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_create_panel | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_create_rawimage | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_create_scrollbar | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_create_scrollview | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_create_slider | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_create_text | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_create_toggle | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_distribute_selected | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| ui_find_all | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_layout_children | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_set_anchor | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_set_image | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_set_rect | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| ui_set_text | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## uitoolkit (25 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| uitk_add_element | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_add_uss_rule | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_clone_element | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_create_batch | x | x | B | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses BatchExecutor / UIToolkitSkills helpers not in _shared |
| uitk_create_document | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_create_editor_window | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_create_from_template | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_create_panel_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_create_runtime_ui | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_create_uss | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_create_uxml | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_delete_file | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_find_files | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_get_panel_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_inspect_document | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_inspect_uxml | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_list_documents | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_list_uss_variables | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_modify_element | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_read_file | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_remove_element | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_remove_uss_rule | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_set_document | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_set_panel_settings | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| uitk_write_file | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## validation (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| validate_cleanup_empty_folders | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| validate_find_missing_scripts | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| validate_find_unused_assets | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| validate_fix_missing_scripts | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| validate_mesh_collider_convex | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| validate_missing_references | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| validate_project_structure | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| validate_scene | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| validate_shader_errors | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green |
| validate_texture_sizes | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## xr (22 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| xr_add_direct_interactor | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_add_grab_interactable | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_add_interaction_event | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_add_ray_interactor | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_add_simple_interactable | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_add_socket_interactor | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_add_teleport_anchor | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_add_teleport_area | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_check_setup | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_configure_haptics | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_configure_interactable | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_configure_interaction_layers | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_get_scene_report | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_list_interactables | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_list_interactors | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_setup_continuous_move | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_setup_event_system | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_setup_interaction_manager | x | x | B | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: #if !XRI gate + undefined NoXRI/XRReflectionHelper upstream helpers; references undeclared name param |
| xr_setup_rig | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_setup_teleportation | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_setup_turn_provider | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| xr_setup_ui_canvas | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

