# Recipe Validation Tracker

State as of 2026-04-21. Updated in-place by `tools/tracker_update.py`; queried by `tools/tracker_next.py`.

## Gates

- **ext** — body re-extracted from upstream (or never stubbed).
- **pre** — `## Prerequisites` section declares every `_shared/*.md` it uses.
- **comp** — passes compile-only Unity_RunCommand (body in `if (false)`).
- **run** — passes end-to-end Unity_RunCommand.

Cell values: `x` = done, `-` = pending, `B` = blocker (see notes).

## Summary

- Total recipes: **484**
- ext: **403** / 484
- pre: **1** / 484
- comp: **1** / 484
- run: **1** / 484

## Domains

[animator](#animator-10-recipes) · [asset](#asset-16-recipes) · [camera](#camera-11-recipes) · [cinemachine](#cinemachine-34-recipes) · [cleaner](#cleaner-10-recipes) · [component](#component-10-recipes) · [console](#console-13-recipes) · [editor](#editor-12-recipes) · [event](#event-10-recipes) · [gameobject](#gameobject-18-recipes) · [importer](#importer-39-recipes) · [light](#light-10-recipes) · [material](#material-21-recipes) · [navmesh](#navmesh-10-recipes) · [optimization](#optimization-10-recipes) · [package](#package-11-recipes) · [perception](#perception-18-recipes) · [physics](#physics-12-recipes) · [prefab](#prefab-11-recipes) · [probuilder](#probuilder-22-recipes) · [project](#project-11-recipes) · [sample](#sample-8-recipes) · [scene](#scene-10-recipes) · [scriptableobject](#scriptableobject-10-recipes) · [shader](#shader-11-recipes) · [smart](#smart-10-recipes) · [terrain](#terrain-10-recipes) · [test](#test-11-recipes) · [timeline](#timeline-12-recipes) · [ui](#ui-26-recipes) · [uitoolkit](#uitoolkit-25-recipes) · [validation](#validation-10-recipes) · [xr](#xr-22-recipes)

## animator (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| animator_add_parameter | x | - | - | - |  |
| animator_add_state | x | - | - | - |  |
| animator_add_transition | x | - | - | - |  |
| animator_assign_controller | x | - | - | - |  |
| animator_create_controller | x | - | - | - |  |
| animator_get_info | x | - | - | - |  |
| animator_get_parameters | x | - | - | - |  |
| animator_list_states | x | - | - | - |  |
| animator_play | x | - | - | - |  |
| animator_set_parameter | x | - | - | - |  |

## asset (16 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| asset_create_folder | x | - | - | - |  |
| asset_delete | x | - | - | - |  |
| asset_delete_batch | x | - | - | - |  |
| asset_duplicate | x | - | - | - |  |
| asset_find | x | - | - | - |  |
| asset_get_info | x | - | - | - |  |
| asset_get_labels | x | - | - | - |  |
| asset_import | x | - | - | - |  |
| asset_import_batch | x | - | - | - |  |
| asset_move | x | - | - | - |  |
| asset_move_batch | x | - | - | - |  |
| asset_refresh | x | - | - | - |  |
| asset_reimport | x | - | - | - |  |
| asset_reimport_batch | x | - | - | - |  |
| asset_set_labels | x | - | - | - |  |
| batch_query_assets | x | - | - | - |  |

## camera (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| camera_align_view_to_object | x | - | - | - |  |
| camera_create | x | - | - | - |  |
| camera_get_info | x | - | - | - |  |
| camera_get_properties | x | - | - | - |  |
| camera_list | x | - | - | - |  |
| camera_look_at | x | - | - | - |  |
| camera_screenshot | x | - | - | - |  |
| camera_set_culling_mask | x | - | - | - |  |
| camera_set_orthographic | x | - | - | - |  |
| camera_set_properties | x | - | - | - |  |
| camera_set_transform | x | - | - | - |  |

## cinemachine (34 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| cinemachine_add_component | x | - | - | - |  |
| cinemachine_add_extension | x | - | - | - |  |
| cinemachine_configure_aim | x | - | - | - |  |
| cinemachine_configure_body | x | - | - | - |  |
| cinemachine_configure_camera_manager | x | - | - | - |  |
| cinemachine_configure_extension | x | - | - | - |  |
| cinemachine_configure_impulse_source | x | - | - | - |  |
| cinemachine_create_clear_shot | x | - | - | - |  |
| cinemachine_create_freelook | x | - | - | - |  |
| cinemachine_create_mixing_camera | x | - | - | - |  |
| cinemachine_create_sequencer | x | - | - | - |  |
| cinemachine_create_state_driven_camera | x | - | - | - |  |
| cinemachine_create_target_group | x | - | - | - |  |
| cinemachine_create_vcam | x | - | - | - |  |
| cinemachine_get_brain_info | x | - | - | - |  |
| cinemachine_impulse_generate | x | - | - | - |  |
| cinemachine_inspect_vcam | x | - | - | - |  |
| cinemachine_list_components | x | - | - | - |  |
| cinemachine_mixing_camera_set_weight | x | - | - | - |  |
| cinemachine_remove_extension | x | - | - | - |  |
| cinemachine_sequencer_add_instruction | x | - | - | - |  |
| cinemachine_set_active | x | - | - | - |  |
| cinemachine_set_blend | x | - | - | - |  |
| cinemachine_set_brain | x | - | - | - |  |
| cinemachine_set_component | x | - | - | - |  |
| cinemachine_set_lens | x | - | - | - |  |
| cinemachine_set_noise | x | - | - | - |  |
| cinemachine_set_priority | x | - | - | - |  |
| cinemachine_set_spline | x | - | - | - |  |
| cinemachine_set_targets | x | - | - | - |  |
| cinemachine_set_vcam_property | x | - | - | - |  |
| cinemachine_state_driven_camera_add_instruction | x | - | - | - |  |
| cinemachine_target_group_add_member | x | - | - | - |  |
| cinemachine_target_group_remove_member | x | - | - | - |  |

## cleaner (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| cleaner_delete_assets | x | - | - | - |  |
| cleaner_delete_empty_folders | x | - | - | - |  |
| cleaner_find_duplicates | x | - | - | - |  |
| cleaner_find_empty_folders | x | - | - | - |  |
| cleaner_find_large_assets | x | - | - | - |  |
| cleaner_find_missing_references | x | - | - | - |  |
| cleaner_find_unused_assets | x | - | - | - |  |
| cleaner_fix_missing_scripts | x | - | - | - |  |
| cleaner_get_asset_usage | x | - | - | - |  |
| cleaner_get_dependency_tree | x | - | - | - |  |

## component (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| component_add | - | - | - | - |  |
| component_add_batch | - | - | - | - |  |
| component_copy | - | - | - | - |  |
| component_get_properties | - | - | - | - |  |
| component_list | - | - | - | - |  |
| component_remove | - | - | - | - |  |
| component_remove_batch | - | - | - | - |  |
| component_set_enabled | - | - | - | - |  |
| component_set_property | - | - | - | - |  |
| component_set_property_batch | - | - | - | - |  |

## console (13 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| console_clear | x | - | - | - |  |
| console_export | x | - | - | - |  |
| console_get_logs | x | - | - | - |  |
| console_get_stats | x | - | - | - |  |
| console_log | x | - | - | - |  |
| console_set_clear_on_play | x | - | - | - |  |
| console_set_collapse | x | - | - | - |  |
| console_set_pause_on_error | x | - | - | - |  |
| console_start_capture | x | - | - | - |  |
| console_stop_capture | x | - | - | - |  |
| debug_force_recompile | x | - | - | - |  |
| debug_get_defines | x | - | - | - |  |
| debug_set_defines | x | - | - | - |  |

## editor (12 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| editor_execute_menu | x | - | - | - |  |
| editor_get_context | x | - | - | - |  |
| editor_get_layers | x | - | - | - |  |
| editor_get_selection | x | - | - | - |  |
| editor_get_state | x | - | - | - |  |
| editor_get_tags | x | - | - | - |  |
| editor_pause | x | - | - | - |  |
| editor_play | x | - | - | - |  |
| editor_redo | x | - | - | - |  |
| editor_select | x | - | - | - |  |
| editor_stop | x | - | - | - |  |
| editor_undo | x | - | - | - |  |

## event (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| event_add_listener | x | - | - | - |  |
| event_add_listener_batch | x | - | - | - |  |
| event_clear_listeners | x | - | - | - |  |
| event_copy_listeners | x | - | - | - |  |
| event_get_listener_count | x | - | - | - |  |
| event_get_listeners | x | - | - | - |  |
| event_invoke | x | - | - | - |  |
| event_list_events | x | - | - | - |  |
| event_remove_listener | x | - | - | - |  |
| event_set_listener_state | x | - | - | - |  |

## gameobject (18 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| gameobject_create | x | x | x | x | 2026-04-21: pilot; 2026-04-21: pilot; 2026-04-21: pilot |
| gameobject_create_batch | - | - | - | - |  |
| gameobject_delete | - | - | - | - |  |
| gameobject_delete_batch | - | - | - | - |  |
| gameobject_duplicate | - | - | - | - |  |
| gameobject_duplicate_batch | - | - | - | - |  |
| gameobject_find | - | - | - | - |  |
| gameobject_get_info | - | - | - | - |  |
| gameobject_rename | - | - | - | - |  |
| gameobject_rename_batch | - | - | - | - |  |
| gameobject_set_active | - | - | - | - |  |
| gameobject_set_active_batch | - | - | - | - |  |
| gameobject_set_layer_batch | - | - | - | - |  |
| gameobject_set_parent | - | - | - | - |  |
| gameobject_set_parent_batch | - | - | - | - |  |
| gameobject_set_tag_batch | - | - | - | - |  |
| gameobject_set_transform | - | - | - | - |  |
| gameobject_set_transform_batch | - | - | - | - |  |

## importer (39 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| asset_reimport | x | - | - | - |  |
| asset_reimport_batch | x | - | - | - |  |
| audio_add_source | x | - | - | - |  |
| audio_create_mixer | x | - | - | - |  |
| audio_find_clips | x | - | - | - |  |
| audio_find_sources_in_scene | x | - | - | - |  |
| audio_get_clip_info | x | - | - | - |  |
| audio_get_import_settings | x | - | - | - |  |
| audio_get_settings | x | - | - | - |  |
| audio_get_source_info | x | - | - | - |  |
| audio_set_import_settings | x | - | - | - |  |
| audio_set_settings | x | - | - | - |  |
| audio_set_settings_batch | x | - | - | - |  |
| audio_set_source_properties | x | - | - | - |  |
| model_find_assets | x | - | - | - |  |
| model_get_animations_info | x | - | - | - |  |
| model_get_import_settings | x | - | - | - |  |
| model_get_materials_info | x | - | - | - |  |
| model_get_mesh_info | x | - | - | - |  |
| model_get_rig_info | x | - | - | - |  |
| model_get_settings | x | - | - | - |  |
| model_set_animation_clips | x | - | - | - |  |
| model_set_import_settings | x | - | - | - |  |
| model_set_rig | x | - | - | - |  |
| model_set_settings | x | - | - | - |  |
| model_set_settings_batch | x | - | - | - |  |
| sprite_set_import_settings | x | - | - | - |  |
| texture_find_assets | x | - | - | - |  |
| texture_find_by_size | x | - | - | - |  |
| texture_get_import_settings | x | - | - | - |  |
| texture_get_info | x | - | - | - |  |
| texture_get_platform_settings | x | - | - | - |  |
| texture_get_settings | x | - | - | - |  |
| texture_set_import_settings | x | - | - | - |  |
| texture_set_platform_settings | x | - | - | - |  |
| texture_set_settings | x | - | - | - |  |
| texture_set_settings_batch | x | - | - | - |  |
| texture_set_sprite_settings | x | - | - | - |  |
| texture_set_type | x | - | - | - |  |

## light (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| light_add_probe_group | x | - | - | - |  |
| light_add_reflection_probe | x | - | - | - |  |
| light_create | x | - | - | - |  |
| light_find_all | x | - | - | - |  |
| light_get_info | x | - | - | - |  |
| light_get_lightmap_settings | x | - | - | - |  |
| light_set_enabled | x | - | - | - |  |
| light_set_enabled_batch | x | - | - | - |  |
| light_set_properties | x | - | - | - |  |
| light_set_properties_batch | x | - | - | - |  |

## material (21 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| material_assign | - | - | - | - |  |
| material_assign_batch | - | - | - | - |  |
| material_create | - | - | - | - |  |
| material_create_batch | - | - | - | - |  |
| material_duplicate | - | - | - | - |  |
| material_get_keywords | - | - | - | - |  |
| material_get_properties | - | - | - | - |  |
| material_set_color | - | - | - | - |  |
| material_set_colors_batch | - | - | - | - |  |
| material_set_emission | - | - | - | - |  |
| material_set_emission_batch | - | - | - | - |  |
| material_set_float | - | - | - | - |  |
| material_set_gi_flags | - | - | - | - |  |
| material_set_int | - | - | - | - |  |
| material_set_keyword | - | - | - | - |  |
| material_set_render_queue | - | - | - | - |  |
| material_set_shader | - | - | - | - |  |
| material_set_texture | - | - | - | - |  |
| material_set_texture_offset | - | - | - | - |  |
| material_set_texture_scale | - | - | - | - |  |
| material_set_vector | - | - | - | - |  |

## navmesh (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| navmesh_add_agent | x | - | - | - |  |
| navmesh_add_obstacle | x | - | - | - |  |
| navmesh_bake | x | - | - | - |  |
| navmesh_calculate_path | x | - | - | - |  |
| navmesh_clear | x | - | - | - |  |
| navmesh_get_settings | x | - | - | - |  |
| navmesh_sample_position | x | - | - | - |  |
| navmesh_set_agent | x | - | - | - |  |
| navmesh_set_area_cost | x | - | - | - |  |
| navmesh_set_obstacle | x | - | - | - |  |

## optimization (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| optimize_analyze_overdraw | x | - | - | - |  |
| optimize_analyze_scene | x | - | - | - |  |
| optimize_audio_compression | x | - | - | - |  |
| optimize_find_duplicate_materials | x | - | - | - |  |
| optimize_find_large_assets | x | - | - | - |  |
| optimize_get_static_flags | x | - | - | - |  |
| optimize_mesh_compression | x | - | - | - |  |
| optimize_set_lod_group | x | - | - | - |  |
| optimize_set_static_flags | x | - | - | - |  |
| optimize_textures | x | - | - | - |  |

## package (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| package_check | x | - | - | - |  |
| package_get_cinemachine_status | x | - | - | - |  |
| package_get_dependencies | x | - | - | - |  |
| package_get_versions | x | - | - | - |  |
| package_install | x | - | - | - |  |
| package_install_cinemachine | x | - | - | - |  |
| package_install_splines | x | - | - | - |  |
| package_list | x | - | - | - |  |
| package_refresh | x | - | - | - |  |
| package_remove | x | - | - | - |  |
| package_search | x | - | - | - |  |

## perception (18 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| hierarchy_describe | x | - | - | - |  |
| project_stack_detect | x | - | - | - |  |
| scene_analyze | x | - | - | - |  |
| scene_component_stats | x | - | - | - |  |
| scene_context | x | - | - | - |  |
| scene_contract_validate | x | - | - | - |  |
| scene_dependency_analyze | x | - | - | - |  |
| scene_diff | x | - | - | - |  |
| scene_export_report | x | - | - | - |  |
| scene_find_hotspots | x | - | - | - |  |
| scene_health_check | x | - | - | - |  |
| scene_materials | x | - | - | - |  |
| scene_performance_hints | x | - | - | - |  |
| scene_spatial_query | x | - | - | - |  |
| scene_summarize | x | - | - | - |  |
| scene_tag_layer_stats | x | - | - | - |  |
| script_analyze | x | - | - | - |  |
| script_dependency_graph | x | - | - | - |  |

## physics (12 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| physics_boxcast | x | - | - | - |  |
| physics_check_overlap | x | - | - | - |  |
| physics_create_material | x | - | - | - |  |
| physics_get_gravity | x | - | - | - |  |
| physics_get_layer_collision | x | - | - | - |  |
| physics_overlap_box | x | - | - | - |  |
| physics_raycast | x | - | - | - |  |
| physics_raycast_all | x | - | - | - |  |
| physics_set_gravity | x | - | - | - |  |
| physics_set_layer_collision | x | - | - | - |  |
| physics_set_material | x | - | - | - |  |
| physics_spherecast | x | - | - | - |  |

## prefab (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| prefab_apply | - | - | - | - |  |
| prefab_apply_overrides | - | - | - | - |  |
| prefab_create | - | - | - | - |  |
| prefab_create_variant | - | - | - | - |  |
| prefab_find_instances | - | - | - | - |  |
| prefab_get_overrides | - | - | - | - |  |
| prefab_instantiate | - | - | - | - |  |
| prefab_instantiate_batch | - | - | - | - |  |
| prefab_revert_overrides | - | - | - | - |  |
| prefab_set_property | - | - | - | - |  |
| prefab_unpack | - | - | - | - |  |

## probuilder (22 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| probuilder_bevel_edges | x | - | - | - |  |
| probuilder_bridge_edges | x | - | - | - |  |
| probuilder_center_pivot | x | - | - | - |  |
| probuilder_combine_meshes | x | - | - | - |  |
| probuilder_conform_normals | x | - | - | - |  |
| probuilder_create_batch | x | - | - | - |  |
| probuilder_create_shape | x | - | - | - |  |
| probuilder_delete_faces | x | - | - | - |  |
| probuilder_detach_faces | x | - | - | - |  |
| probuilder_extrude_edges | x | - | - | - |  |
| probuilder_extrude_faces | x | - | - | - |  |
| probuilder_flip_normals | x | - | - | - |  |
| probuilder_get_info | x | - | - | - |  |
| probuilder_get_vertices | x | - | - | - |  |
| probuilder_merge_faces | x | - | - | - |  |
| probuilder_move_vertices | x | - | - | - |  |
| probuilder_project_uv | x | - | - | - |  |
| probuilder_set_face_material | x | - | - | - |  |
| probuilder_set_material | x | - | - | - |  |
| probuilder_set_vertices | x | - | - | - |  |
| probuilder_subdivide | x | - | - | - |  |
| probuilder_weld_vertices | x | - | - | - |  |

## project (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| project_add_tag | x | - | - | - |  |
| project_get_build_settings | x | - | - | - |  |
| project_get_info | x | - | - | - |  |
| project_get_layers | x | - | - | - |  |
| project_get_packages | x | - | - | - |  |
| project_get_player_settings | x | - | - | - |  |
| project_get_quality_settings | x | - | - | - |  |
| project_get_render_pipeline | x | - | - | - |  |
| project_get_tags | x | - | - | - |  |
| project_list_shaders | x | - | - | - |  |
| project_set_quality_level | x | - | - | - |  |

## sample (8 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| create_cube | - | - | - | - |  |
| create_sphere | - | - | - | - |  |
| delete_object | - | - | - | - |  |
| find_objects_by_name | - | - | - | - |  |
| get_scene_info | - | - | - | - |  |
| set_object_position | - | - | - | - |  |
| set_object_rotation | - | - | - | - |  |
| set_object_scale | - | - | - | - |  |

## scene (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| scene_create | x | - | - | - |  |
| scene_find_objects | x | - | - | - |  |
| scene_get_hierarchy | x | - | - | - |  |
| scene_get_info | x | - | - | - |  |
| scene_get_loaded | x | - | - | - |  |
| scene_load | x | - | - | - |  |
| scene_save | x | - | - | - |  |
| scene_screenshot | x | - | - | - |  |
| scene_set_active | x | - | - | - |  |
| scene_unload | x | - | - | - |  |

## scriptableobject (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| scriptableobject_create | x | - | - | - |  |
| scriptableobject_delete | x | - | - | - |  |
| scriptableobject_duplicate | x | - | - | - |  |
| scriptableobject_export_json | x | - | - | - |  |
| scriptableobject_find | x | - | - | - |  |
| scriptableobject_get | x | - | - | - |  |
| scriptableobject_import_json | x | - | - | - |  |
| scriptableobject_list_types | x | - | - | - |  |
| scriptableobject_set | x | - | - | - |  |
| scriptableobject_set_batch | x | - | - | - |  |

## shader (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| shader_check_errors | - | - | - | - |  |
| shader_create | - | - | - | - |  |
| shader_create_urp | x | - | - | - |  |
| shader_delete | - | - | - | - |  |
| shader_find | - | - | - | - |  |
| shader_get_keywords | - | - | - | - |  |
| shader_get_properties | - | - | - | - |  |
| shader_get_variant_count | - | - | - | - |  |
| shader_list | - | - | - | - |  |
| shader_read | - | - | - | - |  |
| shader_set_global_keyword | x | - | - | - |  |

## smart (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| smart_align_to_ground | - | - | - | - |  |
| smart_distribute | x | - | - | - |  |
| smart_randomize_transform | x | - | - | - |  |
| smart_reference_bind | - | - | - | - |  |
| smart_replace_objects | x | - | - | - |  |
| smart_scene_layout | - | - | - | - |  |
| smart_scene_query | - | - | - | - |  |
| smart_scene_query_spatial | - | - | - | - |  |
| smart_select_by_component | x | - | - | - |  |
| smart_snap_to_grid | x | - | - | - |  |

## terrain (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| terrain_add_hill | x | - | - | - |  |
| terrain_create | x | - | - | - |  |
| terrain_flatten | x | - | - | - |  |
| terrain_generate_perlin | x | - | - | - |  |
| terrain_get_height | x | - | - | - |  |
| terrain_get_info | x | - | - | - |  |
| terrain_paint_texture | x | - | - | - |  |
| terrain_set_height | x | - | - | - |  |
| terrain_set_heights_batch | x | - | - | - |  |
| terrain_smooth | x | - | - | - |  |

## test (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| test_cancel | x | - | - | - |  |
| test_create_editmode | x | - | - | - |  |
| test_create_playmode | x | - | - | - |  |
| test_get_last_result | x | - | - | - |  |
| test_get_result | x | - | - | - |  |
| test_get_summary | x | - | - | - |  |
| test_list | x | - | - | - |  |
| test_list_categories | x | - | - | - |  |
| test_run | x | - | - | - |  |
| test_run_by_name | x | - | - | - |  |
| test_smoke_skills | x | - | - | - |  |

## timeline (12 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| timeline_add_activation_track | x | - | - | - |  |
| timeline_add_animation_track | x | - | - | - |  |
| timeline_add_audio_track | x | - | - | - |  |
| timeline_add_clip | x | - | - | - |  |
| timeline_add_control_track | x | - | - | - |  |
| timeline_add_signal_track | x | - | - | - |  |
| timeline_create | x | - | - | - |  |
| timeline_list_tracks | x | - | - | - |  |
| timeline_play | x | - | - | - |  |
| timeline_remove_track | x | - | - | - |  |
| timeline_set_binding | x | - | - | - |  |
| timeline_set_duration | x | - | - | - |  |

## ui (26 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| ui_add_canvas_group | x | - | - | - |  |
| ui_add_layout_element | x | - | - | - |  |
| ui_add_mask | x | - | - | - |  |
| ui_add_outline | x | - | - | - |  |
| ui_align_selected | x | - | - | - |  |
| ui_configure_selectable | x | - | - | - |  |
| ui_create_batch | x | - | - | - |  |
| ui_create_button | x | - | - | - |  |
| ui_create_canvas | x | - | - | - |  |
| ui_create_dropdown | x | - | - | - |  |
| ui_create_image | x | - | - | - |  |
| ui_create_inputfield | x | - | - | - |  |
| ui_create_panel | x | - | - | - |  |
| ui_create_rawimage | x | - | - | - |  |
| ui_create_scrollbar | x | - | - | - |  |
| ui_create_scrollview | x | - | - | - |  |
| ui_create_slider | x | - | - | - |  |
| ui_create_text | x | - | - | - |  |
| ui_create_toggle | x | - | - | - |  |
| ui_distribute_selected | x | - | - | - |  |
| ui_find_all | x | - | - | - |  |
| ui_layout_children | x | - | - | - |  |
| ui_set_anchor | x | - | - | - |  |
| ui_set_image | x | - | - | - |  |
| ui_set_rect | x | - | - | - |  |
| ui_set_text | x | - | - | - |  |

## uitoolkit (25 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| uitk_add_element | x | - | - | - |  |
| uitk_add_uss_rule | x | - | - | - |  |
| uitk_clone_element | x | - | - | - |  |
| uitk_create_batch | x | - | - | - |  |
| uitk_create_document | x | - | - | - |  |
| uitk_create_editor_window | x | - | - | - |  |
| uitk_create_from_template | x | - | - | - |  |
| uitk_create_panel_settings | x | - | - | - |  |
| uitk_create_runtime_ui | x | - | - | - |  |
| uitk_create_uss | x | - | - | - |  |
| uitk_create_uxml | x | - | - | - |  |
| uitk_delete_file | x | - | - | - |  |
| uitk_find_files | x | - | - | - |  |
| uitk_get_panel_settings | x | - | - | - |  |
| uitk_inspect_document | x | - | - | - |  |
| uitk_inspect_uxml | x | - | - | - |  |
| uitk_list_documents | x | - | - | - |  |
| uitk_list_uss_variables | x | - | - | - |  |
| uitk_modify_element | x | - | - | - |  |
| uitk_read_file | x | - | - | - |  |
| uitk_remove_element | x | - | - | - |  |
| uitk_remove_uss_rule | x | - | - | - |  |
| uitk_set_document | x | - | - | - |  |
| uitk_set_panel_settings | x | - | - | - |  |
| uitk_write_file | x | - | - | - |  |

## validation (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| validate_cleanup_empty_folders | x | - | - | - |  |
| validate_find_missing_scripts | x | - | - | - |  |
| validate_find_unused_assets | x | - | - | - |  |
| validate_fix_missing_scripts | x | - | - | - |  |
| validate_mesh_collider_convex | x | - | - | - |  |
| validate_missing_references | x | - | - | - |  |
| validate_project_structure | x | - | - | - |  |
| validate_scene | x | - | - | - |  |
| validate_shader_errors | x | - | - | - |  |
| validate_texture_sizes | x | - | - | - |  |

## xr (22 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| xr_add_direct_interactor | x | - | - | - |  |
| xr_add_grab_interactable | x | - | - | - |  |
| xr_add_interaction_event | x | - | - | - |  |
| xr_add_ray_interactor | x | - | - | - |  |
| xr_add_simple_interactable | x | - | - | - |  |
| xr_add_socket_interactor | x | - | - | - |  |
| xr_add_teleport_anchor | x | - | - | - |  |
| xr_add_teleport_area | x | - | - | - |  |
| xr_check_setup | x | - | - | - |  |
| xr_configure_haptics | x | - | - | - |  |
| xr_configure_interactable | x | - | - | - |  |
| xr_configure_interaction_layers | x | - | - | - |  |
| xr_get_scene_report | x | - | - | - |  |
| xr_list_interactables | x | - | - | - |  |
| xr_list_interactors | x | - | - | - |  |
| xr_setup_continuous_move | x | - | - | - |  |
| xr_setup_event_system | x | - | - | - |  |
| xr_setup_interaction_manager | x | - | - | - |  |
| xr_setup_rig | x | - | - | - |  |
| xr_setup_teleportation | x | - | - | - |  |
| xr_setup_turn_provider | x | - | - | - |  |
| xr_setup_ui_canvas | x | - | - | - |  |

