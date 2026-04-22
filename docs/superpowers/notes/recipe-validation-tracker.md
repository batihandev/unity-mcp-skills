# Recipe Validation Tracker

State as of 2026-04-21. Updated in-place by `tools/tracker_update.py`; queried by `tools/tracker_next.py`.

## Gates

- **ext** — body re-extracted from upstream (or never stubbed).
- **pre** — `## Prerequisites` section declares every `_shared/*.md` it uses.
- **comp** — passes compile-only Unity_RunCommand (body in `if (false)`).
- **run** — passes end-to-end Unity_RunCommand.

Cell values: `x` = done, `-` = pending, `B` = blocker (see notes), `R` = retired (all gates share this value; notes column records the redirect target — MCP tool name or replacement recipe path).

## Summary

- Total recipes: **485**
- ext: **457** / 485
- pre: **457** / 485
- comp: **364** / 485
- run: **33** / 485
- retired: **22** / 485

## Domains

[animator](#animator-10-recipes) · [asset](#asset-16-recipes) · [camera](#camera-11-recipes) · [cinemachine](#cinemachine-34-recipes) · [cleaner](#cleaner-10-recipes) · [component](#component-10-recipes) · [console](#console-13-recipes) · [editor](#editor-12-recipes) · [event](#event-10-recipes) · [gameobject](#gameobject-18-recipes) · [importer](#importer-39-recipes) · [light](#light-10-recipes) · [material](#material-21-recipes) · [navmesh](#navmesh-10-recipes) · [optimization](#optimization-10-recipes) · [package](#package-11-recipes) · [perception](#perception-18-recipes) · [physics](#physics-12-recipes) · [prefab](#prefab-11-recipes) · [probuilder](#probuilder-22-recipes) · [project](#project-11-recipes) · [sample](#sample-8-recipes) · [scene](#scene-10-recipes) · [scriptableobject](#scriptableobject-10-recipes) · [shader](#shader-11-recipes) · [smart](#smart-10-recipes) · [terrain](#terrain-10-recipes) · [test](#test-11-recipes) · [timeline](#timeline-12-recipes) · [ui](#ui-26-recipes) · [uitoolkit](#uitoolkit-25-recipes) · [validation](#validation-10-recipes) · [xr](#xr-22-recipes)

## animator (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| animator_add_parameter | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| animator_add_state | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-22: Task 20: comp-smoke live |
| animator_add_transition | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| animator_assign_controller | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| animator_create_controller | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| animator_get_info | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| animator_get_parameters | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| animator_list_states | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| animator_play | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| animator_set_parameter | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |

## asset (16 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| asset_create_folder | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |
| asset_delete | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: dropped ServerAvailabilityHelper (REST-era). asset_delete: DeleteAsset → MoveAssetToTrash.; 2026-04-22: already uses MoveAssetToTrash |
| asset_delete_batch | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15 rewrite applied (BatchExecutor → foreach) but cannot smoke-verify: Unity_RunCommand MCP analyzer rejects any module containing AssetDatabase.DeleteAsset even inside if(false) — 'User interactions are not supported'. Same guard that blocked shader_delete. Recipe code is structurally correct.; 2026-04-22: Task 15 complete: switched DeleteAsset → MoveAssetToTrash (analyzer-safe, restorable). Run-verified: deleted throwaway Assets/_ThrowawayTest/ToDelete_B.mat; 2026-04-22: Run-verified 2026-04-22 via throwaway materials; 2026-04-22: already uses MoveAssetToTrash |
| asset_duplicate | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: dropped ServerAvailabilityHelper (REST-era). asset_delete: DeleteAsset → MoveAssetToTrash.; 2026-04-22: clean |
| asset_find | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |
| asset_get_info | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |
| asset_get_labels | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |
| asset_import | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: dropped ServerAvailabilityHelper (REST-era). asset_delete: DeleteAsset → MoveAssetToTrash.; 2026-04-22: clean |
| asset_import_batch | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; ServerAvailabilityHelper dropped (REST-era); 2026-04-22: clean |
| asset_move | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: dropped ServerAvailabilityHelper (REST-era). asset_delete: DeleteAsset → MoveAssetToTrash.; 2026-04-22: clean |
| asset_move_batch | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; ServerAvailabilityHelper dropped (REST-era); 2026-04-22: clean |
| asset_refresh | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: dropped ServerAvailabilityHelper (REST-era). asset_delete: DeleteAsset → MoveAssetToTrash.; 2026-04-22: clean |
| asset_reimport | x | x | x | - | 2026-04-22: dropped ServerAvailabilityHelper (REST-era); 2026-04-22: clean |
| asset_reimport_batch | x | x | x | - | 2026-04-22: dropped ServerAvailabilityHelper (REST-era); 2026-04-22: clean |
| asset_set_labels | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |
| batch_query_assets | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: recipe body is top-level REPL script, not CommandScript; uses Newtonsoft.Json which is unavailable in Unity_RunCommand; 2026-04-21: retired → native MCP |

## camera (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| camera_align_view_to_object | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |
| camera_create | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |
| camera_get_info | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |
| camera_get_properties | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |
| camera_list | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |
| camera_look_at | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-22: clean |
| camera_screenshot | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → native MCP; 2026-04-21: restored from retirement (Unity_Camera_Capture has different surface: in-memory only, no width/height, instanceID-only); 2026-04-22: clean |
| camera_set_culling_mask | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |
| camera_set_orthographic | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |
| camera_set_properties | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |
| camera_set_transform | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: clean |

## cinemachine (34 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| cinemachine_add_component | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: direct CinemachineCamera API |
| cinemachine_add_extension | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: direct CinemachineExtension type probe |
| cinemachine_configure_aim | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: CinemachineRotationComposer.Composition direct API |
| cinemachine_configure_body | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: CinemachineFollow.FollowOffset direct API |
| cinemachine_configure_camera_manager | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: ClearShot/StateDriven/Sequencer direct API |
| cinemachine_configure_extension | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: Confiner/FollowZoom/GroupFraming reflection direct |
| cinemachine_configure_impulse_source | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: ImpulseDefinition nested reflection |
| cinemachine_create_clear_shot | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: Undo.AddComponent<CinemachineClearShot> |
| cinemachine_create_freelook | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: CM3 FreeLook rig direct API |
| cinemachine_create_mixing_camera | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: Undo.AddComponent<CinemachineMixingCamera> |
| cinemachine_create_sequencer | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: CinemachineSequencerCamera.Loop direct API |
| cinemachine_create_state_driven_camera | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: CinemachineStateDrivenCamera.AnimatedTarget direct |
| cinemachine_create_target_group | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: CinemachineTargetGroup direct |
| cinemachine_create_vcam | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: CinemachineCamera + CinemachineBrain direct API |
| cinemachine_get_brain_info | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: CinemachineBrain.ActiveVirtualCamera/ActiveBlend direct; 2026-04-22: Task 21: live — returns clean 'no CinemachineBrain' error when scene has no brain (expected path) |
| cinemachine_impulse_generate | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: source.GenerateImpulse + JsonUtility direct |
| cinemachine_inspect_vcam | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: CinemachineCamera.Follow/LookAt/Priority.Value/Lens direct; 2026-04-22: Task 21: live — returns clean 'no VCam' error when target absent |
| cinemachine_list_components | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses CinemachineAdapter.CmAssembly — upstream helper not ported; 2026-04-22: Task 16: fully-qualify ReflectionTypeLoadException to dodge reformatter NRE; 2026-04-22: Task 21: live — 68 Cinemachine component types enumerated |
| cinemachine_mixing_camera_set_weight | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: mixer.SetWeight direct API |
| cinemachine_remove_extension | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: type probe + Undo.DestroyObjectImmediate direct |
| cinemachine_sequencer_add_instruction | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: seq.Instructions + Instruction/BlendDefinition direct |
| cinemachine_set_active | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: Priority.Value max+1 direct |
| cinemachine_set_blend | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: CinemachineBrain.DefaultBlend direct |
| cinemachine_set_brain | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: CinemachineBrain all props direct |
| cinemachine_set_component | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: vcam.GetCinemachineComponent direct |
| cinemachine_set_lens | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: vcam.Lens direct API |
| cinemachine_set_noise | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: CinemachineBasicMultiChannelPerlin direct |
| cinemachine_set_priority | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: direct CM3 CinemachineCamera.Priority.Value (Task 14 pilot) |
| cinemachine_set_spline | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: CinemachineSplineDolly.Spline direct |
| cinemachine_set_targets | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: vcam.Follow/LookAt direct |
| cinemachine_set_vcam_property | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: GetFields()+FirstOrDefault + Convert.ChangeType direct |
| cinemachine_state_driven_camera_add_instruction | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: StateDrivenCamera.Instructions array direct |
| cinemachine_target_group_add_member | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: TargetGroup.AddMember/RemoveMember direct |
| cinemachine_target_group_remove_member | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: TargetGroup.RemoveMember direct |

## cleaner (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| cleaner_delete_assets | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| cleaner_delete_empty_folders | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| cleaner_find_duplicates | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live after usings + SetResult + removing dynamic cast (analyzer trap) |
| cleaner_find_empty_folders | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live after SetValue→SetResult + usings |
| cleaner_find_large_assets | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| cleaner_find_missing_references | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| cleaner_find_unused_assets | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live after adding System.Collections.Generic/IO/Linq usings + SetValue→SetResult fix |
| cleaner_fix_missing_scripts | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| cleaner_get_asset_usage | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| cleaner_get_dependency_tree | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |

## component (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| component_add | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: restored param locals; routed FindComponentType via _shared/component_type_finder; inlined GetSimilarTypes/AllowMultiple |
| component_add_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke green |
| component_copy | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke green |
| component_get_properties | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke green; no BindingFlags |
| component_list | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke green |
| component_remove | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke green |
| component_remove_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach |
| component_set_enabled | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke green |
| component_set_property | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke green; prereqs need merged ComponentSkills (both FindComponentType+ConvertValue) |
| component_set_property_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach |

## console (13 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| console_clear | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → native MCP |
| console_export | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: rewritten: StartGettingEntries/GetEntryInternal, GetField('message'/'mode'), no BindingFlags |
| console_get_logs | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → native MCP |
| console_get_stats | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: rewritten: LogEntries.GetCountsByType via GetMethods()+foreach |
| console_log | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: trivial Debug.Log/LogWarning/LogError switch |
| console_set_clear_on_play | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: removed BindingFlags; GetMethods()+foreach for SetConsoleFlag, EditorPrefs fallback |
| console_set_collapse | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: same pattern as set_clear_on_play, bit 32 |
| console_set_pause_on_error | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: s_ConsoleFlags unreachable; simplified to EditorPrefs only |
| console_start_capture | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: rewritten: stateless EditorPrefs marker |
| console_stop_capture | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: rewritten: clears EditorPrefs marker |
| debug_force_recompile | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: dropped ServerAvailabilityHelper; fully-qualified UnityEditor.Compilation.CompilationPipeline (namespace collision in Unity_RunCommand wrapper); 2026-04-23: fully-qualified CompilationPipeline call passes cleanly |
| debug_get_defines | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-21: defines returned (Task 21); 2026-04-23: EditorUserBuildSettings+PlayerSettings only |
| debug_set_defines | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: dropped ServerAvailabilityHelper; 2026-04-23: PlayerSettings.SetScriptingDefineSymbolsForGroup |

## editor (12 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| editor_execute_menu | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: EditorApplication.ExecuteMenuItem, clean |
| editor_get_context | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: Selection+AssetDatabase+SceneManager, GameObjectFinder.GetPath stub |
| editor_get_layers | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: Enumerable.Range LINQ, clean |
| editor_get_selection | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: Selection.gameObjects LINQ, clean |
| editor_get_state | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: EditorApplication properties only |
| editor_get_tags | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-21: 9 tags returned (Task 21); 2026-04-23: InternalEditorUtility.tags, clean |
| editor_pause | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: EditorApplication.isPaused toggle |
| editor_play | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: Task 5 async redesign; 2026-04-23: EditorApplication.isPlaying, clean |
| editor_redo | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: Undo.PerformRedo, clean |
| editor_select | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: Selection.activeGameObject+GameObjectFinder.FindOrError |
| editor_stop | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: EditorApplication.isPlaying=false, clean |
| editor_undo | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: Undo.PerformUndo, clean |

## event (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| event_add_listener | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live after System.Reflection directive removal + BindingFlags→no-arg walk |
| event_add_listener_batch | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses Newtonsoft JsonConvert + undefined SkillResultHelper; body contains NotImplementedException; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-22: Task 20: comp-smoke live after System.Reflection directive removal + BindingFlags→no-arg walk |
| event_clear_listeners | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live after System.Reflection directive removal + BindingFlags→no-arg walk |
| event_copy_listeners | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live after System.Reflection directive removal + BindingFlags→no-arg walk |
| event_get_listener_count | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live after System.Reflection directive removal + BindingFlags→no-arg walk |
| event_get_listeners | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live after System.Reflection directive removal + BindingFlags→no-arg walk |
| event_invoke | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live after System.Reflection directive removal + BindingFlags→no-arg walk |
| event_list_events | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live after System.Reflection directive removal + BindingFlags→no-arg walk |
| event_remove_listener | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live after System.Reflection directive removal + BindingFlags→no-arg walk |
| event_set_listener_state | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live after System.Reflection directive removal + BindingFlags→no-arg walk |

## gameobject (18 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| gameobject_create | x | x | x | x | 2026-04-21: pilot; 2026-04-21: pilot; 2026-04-21: pilot; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| gameobject_create_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-23: smoke pass |
| gameobject_delete | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| gameobject_delete_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-23: smoke pass |
| gameobject_duplicate | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| gameobject_duplicate_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-23: smoke pass |
| gameobject_find | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: removed Regex; string.IndexOf only |
| gameobject_get_info | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: added using System.Collections.Generic |
| gameobject_rename | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| gameobject_rename_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-23: smoke pass |
| gameobject_set_active | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| gameobject_set_active_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| gameobject_set_layer_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-23: smoke pass |
| gameobject_set_parent | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| gameobject_set_parent_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-23: smoke pass |
| gameobject_set_tag_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-23: smoke pass |
| gameobject_set_transform | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: added missing vars + TryMergeVector2/3 helpers |
| gameobject_set_transform_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: BatchExecutor removed, typed _BatchTransformItem + foreach (Task 15 pilot); 2026-04-23: smoke pass |

## importer (39 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| asset_reimport | R | R | R | - | 2026-04-22: retired — duplicate of recipes/asset/asset_reimport.md; file deleted |
| asset_reimport_batch | R | R | R | - | 2026-04-22: retired — duplicate of recipes/asset/asset_reimport_batch.md; file deleted |
| audio_add_source | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; added execution_result prereq; 2026-04-23: fixed return→SetResult; smoke passed |
| audio_create_mixer | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult + BindingFlags→GetMethods() foreach; 2026-04-23: fixed return→SetResult + BindingFlags→GetMethods foreach; smoke passed |
| audio_find_clips | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; added execution_result prereq; 2026-04-23: fixed return→SetResult; smoke passed |
| audio_find_sources_in_scene | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; added execution_result prereq; 2026-04-23: fixed return→SetResult; smoke passed |
| audio_get_clip_info | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; added execution_result prereq; 2026-04-23: fixed return→SetResult; smoke passed |
| audio_get_import_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; added execution_result prereq; 2026-04-23: fixed return→SetResult; smoke passed |
| audio_get_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; added execution_result prereq; 2026-04-23: fixed return→SetResult; smoke passed |
| audio_get_source_info | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; added execution_result prereq; 2026-04-23: fixed return→SetResult; smoke passed |
| audio_set_import_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; added execution_result prereq; 2026-04-23: fixed return→SetResult; smoke passed |
| audio_set_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; added execution_result prereq; 2026-04-23: fixed return→SetResult; smoke passed |
| audio_set_settings_batch | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-23: smoke pass; already used SetResult; 2026-04-23: already correct; smoke passed |
| audio_set_source_properties | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; added execution_result prereq; 2026-04-23: fixed return→SetResult; smoke passed |
| model_find_assets | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke passed |
| model_get_animations_info | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke passed |
| model_get_import_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke passed |
| model_get_materials_info | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed OfType<Mesh>→OfType<UnityEngine.Mesh>; smoke passed |
| model_get_mesh_info | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke passed; UnityEngine.Mesh qualified in recipe |
| model_get_rig_info | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke passed |
| model_get_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke passed |
| model_set_animation_clips | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: dropped Newtonsoft.Json; hand-parsed manifest/typed-array items; 2026-04-23: smoke passed |
| model_set_import_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke passed |
| model_set_rig | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke passed |
| model_set_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke passed |
| model_set_settings_batch | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-23: smoke passed |
| sprite_set_import_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; smoke passed |
| texture_find_assets | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; smoke passed |
| texture_find_by_size | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; smoke passed |
| texture_get_import_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; smoke passed |
| texture_get_info | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; smoke passed |
| texture_get_platform_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; smoke passed |
| texture_get_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; smoke passed |
| texture_set_import_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; smoke passed |
| texture_set_platform_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; smoke passed |
| texture_set_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; smoke passed |
| texture_set_settings_batch | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-23: already correct; smoke passed |
| texture_set_sprite_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; smoke passed |
| texture_set_type | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: fixed return→SetResult; smoke passed |

## light (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| light_add_probe_group | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| light_add_reflection_probe | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| light_create | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| light_find_all | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| light_get_info | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| light_get_lightmap_settings | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-21: Task 21; 2026-04-22: Task 20: comp-smoke live |
| light_set_enabled | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| light_set_enabled_batch | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-22: Task 20: comp-smoke live |
| light_set_properties | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| light_set_properties_batch | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-22: Task 20: comp-smoke live |

## material (21 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| material_assign | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| material_assign_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses BatchExecutor + SkillResultHelper not in _shared; 2026-04-21: BatchExecutor removed, typed item + foreach (Task 15 pilot); 2026-04-23: smoke pass |
| material_create | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: inlined pipeline probe + FindMaterial (dropped ProjectSkills upstream helper); 2026-04-23: smoke pass |
| material_create_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-23: smoke pass |
| material_duplicate | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| material_get_keywords | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| material_get_properties | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| material_set_color | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: inlined pipeline probe + FindMaterial (dropped ProjectSkills upstream helper); 2026-04-23: smoke pass |
| material_set_colors_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-23: smoke pass |
| material_set_emission | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| material_set_emission_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 15: BatchExecutor → typed _BatchFooItem foreach; 2026-04-23: smoke pass |
| material_set_float | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| material_set_gi_flags | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| material_set_int | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| material_set_keyword | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| material_set_render_queue | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| material_set_shader | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| material_set_texture | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: inlined pipeline probe + FindMaterial (dropped ProjectSkills upstream helper); 2026-04-23: smoke pass |
| material_set_texture_offset | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: inlined pipeline probe + FindMaterial (dropped ProjectSkills upstream helper); 2026-04-23: smoke pass |
| material_set_texture_scale | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: inlined pipeline probe + FindMaterial (dropped ProjectSkills upstream helper); 2026-04-23: smoke pass |
| material_set_vector | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |

## navmesh (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| navmesh_add_agent | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| navmesh_add_obstacle | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| navmesh_bake | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-21: NavMeshSurface rewrite (Task 14); 2026-04-22: Task 20: comp-smoke live |
| navmesh_calculate_path | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| navmesh_clear | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: NavMeshSurface rewrite (Task 14); 2026-04-22: Task 20: comp-smoke live |
| navmesh_get_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| navmesh_sample_position | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| navmesh_set_agent | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| navmesh_set_area_cost | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| navmesh_set_obstacle | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |

## optimization (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| optimize_analyze_overdraw | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| optimize_analyze_scene | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| optimize_audio_compression | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| optimize_find_duplicate_materials | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| optimize_find_large_assets | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-21: 5 large assets (Task 21) |
| optimize_get_static_flags | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| optimize_mesh_compression | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| optimize_set_lod_group | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| optimize_set_static_flags | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |
| optimize_textures | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: smoke pass |

## package (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| package_check | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → native MCP (Unity_PackageManager_*) |
| package_get_cinemachine_status | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → native MCP (Unity_PackageManager_*) |
| package_get_dependencies | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → native MCP (Unity_PackageManager_*) |
| package_get_versions | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → native MCP (Unity_PackageManager_*) |
| package_install | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → native MCP (Unity_PackageManager_*) |
| package_install_cinemachine | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → native MCP (Unity_PackageManager_*) |
| package_install_splines | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → native MCP (Unity_PackageManager_*) |
| package_list | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses PackageManagerHelper.InstalledPackages — upstream helper not ported; 2026-04-21: retired → native MCP (Unity_PackageManager_*) |
| package_refresh | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → native MCP (Unity_PackageManager_*) |
| package_remove | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → native MCP (Unity_PackageManager_*) |
| package_search | R | R | R | R | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → native MCP (Unity_PackageManager_*) |

## perception (18 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| hierarchy_describe | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: SetValue→SetResult; added BuildHierarchyTree impl |
| project_stack_detect | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Diagnostic aggregator: depends on 6+ upstream private helpers (CollectSceneMetrics, ReadInstalledPackageIds, FindTypeInAssemblies, DetermineUiRoute, DetectInputHandling, DetermineProjectProfile) plus ProjectSkills surface. Full inline would 3-4x the recipe. Also calls result.SetValue (wrong API). Follow-up task: either fully inline (~200 lines) or split into narrower recipes (project_get_render_pipeline, project_get_input_system, project_get_ui_route).; 2026-04-22: Created _shared/project_skills.md + _shared/perception_helpers.md for shared upstream surface; recipe now uses ProjectSkills.* + PerceptionHelpers.* prefixes. Helpers live-smoked (URP + 59 packages detected). |
| scene_analyze | R | R | R | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Retired: meta-aggregator delegating to scene_component_stats + scene_health_check + scene_contract_validate + project_stack_detect. Agents call the 4 recipes sequentially. |
| scene_component_stats | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: uses _shared/perception_helpers (CollectSceneMetrics/CollectHotspots/DeduplicateFindings/BuildSuggestedNextSkills/ParseOptionalStringArray/ContainsIgnoreCase/GetPropertyValue/BuildTopComponents); SetValue→SetResult; 2026-04-22: Task 21: live — CollectSceneMetrics walks 87 scene objects cleanly; top components RectTransform(57)/CanvasRenderer(47)/Image(26); all downstream recipe logic is pure transform over this dict |
| scene_context | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: top-level internal struct _CodeDepEdge_sc; SetValue→SetResult; stubs added |
| scene_contract_validate | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: uses _shared/perception_helpers (CollectSceneMetrics/CollectHotspots/DeduplicateFindings/BuildSuggestedNextSkills/ParseOptionalStringArray/ContainsIgnoreCase/GetPropertyValue/BuildTopComponents); SetValue→SetResult; 2026-04-22: Task 21: live — CollectSceneMetrics walks 87 scene objects cleanly; top components RectTransform(57)/CanvasRenderer(47)/Image(26); all downstream recipe logic is pure transform over this dict |
| scene_dependency_analyze | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: top-level internal struct _DepEdge_sda; SetValue→SetResult; stubs added |
| scene_diff | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Diagnostic aggregator: snapshots scene + diffs; ~135-line recipe with upstream private helpers. Inline scope deferred; similar treatment to project_stack_detect.; 2026-04-22: Rewrite: typed _SceneDiffEntry + CaptureSceneSnapshot inline; replaced Newtonsoft.Json with hand-parsed snapshot JSON; SetValue→SetResult |
| scene_export_report | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: top-level internal struct _DepEdge_ser; SetValue→SetResult; stubs added |
| scene_find_hotspots | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: uses _shared/perception_helpers (CollectSceneMetrics/CollectHotspots/DeduplicateFindings/BuildSuggestedNextSkills/ParseOptionalStringArray/ContainsIgnoreCase/GetPropertyValue/BuildTopComponents); SetValue→SetResult; 2026-04-22: Task 21: live — CollectSceneMetrics walks 87 scene objects cleanly; top components RectTransform(57)/CanvasRenderer(47)/Image(26); all downstream recipe logic is pure transform over this dict |
| scene_health_check | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: uses _shared/perception_helpers (CollectSceneMetrics/CollectHotspots/DeduplicateFindings/BuildSuggestedNextSkills/ParseOptionalStringArray/ContainsIgnoreCase/GetPropertyValue/BuildTopComponents); SetValue→SetResult; 2026-04-22: Task 21: live — CollectSceneMetrics walks 87 scene objects cleanly; top components RectTransform(57)/CanvasRenderer(47)/Image(26); all downstream recipe logic is pure transform over this dict |
| scene_materials | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: SetValue→SetResult; added _MatInfo_sm class |
| scene_performance_hints | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: SetValue→SetResult |
| scene_spatial_query | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: SetValue→SetResult (2 occurrences) |
| scene_summarize | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: SetValue→SetResult |
| scene_tag_layer_stats | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: SetValue→SetResult |
| script_analyze | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: 2026-04-22: Task 20: full rewrite — BindingFlags removed, missing helpers added, result.SetValue→SetResult, static string[] for callbacks |
| script_dependency_graph | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: 2026-04-22: Task 20: full rewrite — BindingFlags removed, missing helpers added, HashSet→List for dict values, result.SetValue→SetResult |

## physics (12 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| physics_boxcast | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| physics_check_overlap | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| physics_create_material | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Uses PhysicsMaterial (Unity 6+) correctly; prose false-positive on scan; 2026-04-23: clean; uses PhysicsMaterial (Unity 6+) |
| physics_get_gravity | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-21: (0,-9.81,0) returned (Task 21); 2026-04-23: clean |
| physics_get_layer_collision | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| physics_overlap_box | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| physics_raycast | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| physics_raycast_all | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| physics_set_gravity | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| physics_set_layer_collision | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| physics_set_material | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Uses PhysicsMaterial (Unity 6+) correctly; prose false-positive on scan; 2026-04-23: clean; uses PhysicsMaterial (Unity 6+) |
| physics_spherecast | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |

## prefab (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| prefab_apply | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: added missing variable declarations |
| prefab_apply_overrides | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: added missing variable declarations |
| prefab_create | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: added missing variable declarations; added using System.IO |
| prefab_create_variant | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: body references undeclared sourcePrefabPath/variantPath parameters; missing using System.IO; 2026-04-22: Fixed: restored dropped sourcePrefabPath/variantPath parameter locals; added missing using System.IO |
| prefab_find_instances | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: added missing variable declarations; added using System.Linq |
| prefab_get_overrides | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: added missing variable declarations |
| prefab_instantiate | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: added missing variable declarations |
| prefab_instantiate_batch | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: BatchExecutor removed, typed _PrefabInstantiateItem + foreach (Task 15 pilot) |
| prefab_revert_overrides | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: added missing variable declarations |
| prefab_set_property | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: added variable declarations; added 3 helper methods (FindSerializedProperty/SetSerializedPropertyValue/ListSerializedProperties); added value_converter prereq |
| prefab_unpack | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: added missing variable declarations |

## probuilder (22 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| probuilder_bevel_edges | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: direct ProBuilder API + Bevel.BevelEdges; dirty-domain re-verify |
| probuilder_bridge_edges | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: pbMesh.Bridge direct API |
| probuilder_center_pivot | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: pbMesh.SetPivot/CenterPivot direct API |
| probuilder_combine_meshes | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: bridge-stub extract carries upstream issues: local 'result' var shadows ExecutionResult param; uses #if !PROBUILDER + upstream helpers (NoProBuilder, CombineMeshes.Combine) not in _shared; 2026-04-22: Task 16: CombineMeshes.Combine direct API; previous B cleared |
| probuilder_conform_normals | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: pbMesh.ConformNormals direct API |
| probuilder_create_batch | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: promoted _PBBatchItem to top-level (CS1527 fix); ShapeFactory.Instantiate direct API |
| probuilder_create_shape | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: ShapeFactory.Instantiate direct API |
| probuilder_delete_faces | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: pbMesh.DeleteFaces direct API |
| probuilder_detach_faces | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: pbMesh.DetachFaces direct API |
| probuilder_extrude_edges | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: pbMesh.Extrude(edges) direct API |
| probuilder_extrude_faces | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: pbMesh.Extrude(faces, method) direct API |
| probuilder_flip_normals | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: face.Reverse() direct API |
| probuilder_get_info | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: PropertyInfo gotcha: fully-qualify static fields (Task 14 pilot); 2026-04-22: Task 21: live — clean error path when no ProBuilder mesh; happy-path body already comp-verified via direct ProBuilder API |
| probuilder_get_vertices | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: pbMesh.positions direct API; 2026-04-22: Task 21: live — clean error path when no ProBuilder mesh; happy-path body already comp-verified via direct ProBuilder API |
| probuilder_merge_faces | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: MergeElements.Merge direct API |
| probuilder_move_vertices | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: pbMesh.positions = array direct API |
| probuilder_project_uv | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: GetMethods()+filter (avoid GetMethod+BindingFlags reformatter NRE) |
| probuilder_set_face_material | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: face.submeshIndex + renderer.sharedMaterials direct API |
| probuilder_set_material | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: renderer.sharedMaterial direct API |
| probuilder_set_vertices | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: promoted _VertexPos top-level; pbMesh.positions = array |
| probuilder_subdivide | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: ConnectElements.Connect direct API |
| probuilder_weld_vertices | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: pbMesh.WeldVertices direct API |

## project (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| project_add_tag | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| project_get_build_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| project_get_info | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| project_get_layers | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-21: 8 layers returned (Task 21); 2026-04-23: clean |
| project_get_packages | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: dropped Newtonsoft.Json; hand-parsed manifest/typed-array items; 2026-04-22: Task 21: live run — 59 deps parsed from Packages/manifest.json, firstPkg=com.unity.2d.animation; 2026-04-23: clean |
| project_get_player_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| project_get_quality_settings | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| project_get_render_pipeline | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| project_get_tags | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| project_list_shaders | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |
| project_set_quality_level | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-23: clean |

## sample (8 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| create_cube | R | R | R | R | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → recipes/gameobject/* or recipes/scene/* |
| create_sphere | R | R | R | R | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → recipes/gameobject/* or recipes/scene/* |
| delete_object | R | R | R | R | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → recipes/gameobject/* or recipes/scene/* |
| find_objects_by_name | R | R | R | R | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → recipes/gameobject/* or recipes/scene/* |
| get_scene_info | R | R | R | R | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-21: retired → recipes/gameobject/* or recipes/scene/* |
| set_object_position | R | R | R | R | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → recipes/gameobject/* or recipes/scene/* |
| set_object_rotation | R | R | R | R | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → recipes/gameobject/* or recipes/scene/* |
| set_object_scale | R | R | R | R | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: retired → recipes/gameobject/* or recipes/scene/* |

## scene (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| scene_create | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| scene_find_objects | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| scene_get_hierarchy | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| scene_get_info | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| scene_get_loaded | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| scene_load | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-22: Task 20: comp-smoke live |
| scene_save | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| scene_screenshot | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| scene_set_active | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |
| scene_unload | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live |

## scriptableobject (10 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| scriptableobject_create | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: 2026-04-22: Task 20: comp-smoke green; inlined FindScriptableObjectType; DeleteAsset→MoveAssetToTrash |
| scriptableobject_delete | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: 2026-04-22: Task 20: comp-smoke green; DeleteAsset→MoveAssetToTrash |
| scriptableobject_duplicate | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: 2026-04-22: Task 20: comp-smoke green; clean |
| scriptableobject_export_json | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: 2026-04-22: Task 20: comp-smoke green; clean |
| scriptableobject_find | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-21: 1149 SOs (Task 21) |
| scriptableobject_get | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live, BindingFlags→no-arg |
| scriptableobject_import_json | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: 2026-04-22: Task 20: comp-smoke green; clean |
| scriptableobject_list_types | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: 2026-04-22: Task 20: comp-smoke green; clean |
| scriptableobject_set | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 20: comp-smoke live, BindingFlags→no-arg |
| scriptableobject_set_batch | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Pre-Task-20: dropped Newtonsoft.Json; hand-parsed manifest/typed-array items |

## shader (11 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| shader_check_errors | x | x | x | x | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses FindShaderByNameOrPath — private upstream helper not in _shared; 2026-04-22: Task 16: inlined FindShaderByNameOrPath as private static; 2026-04-22: Task 21: live — URP/Lit shader: 0 compile messages |
| shader_create | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| shader_create_urp | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |
| shader_delete | x | x | x | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: Unity MCP returns 'User interactions not supported' on repeat attempts; compile-only appears blocked at this recipe; deferred; 2026-04-22: Task 16 follow-up: DeleteAsset → MoveAssetToTrash (analyzer-safe); added missing using System.IO for File.Exists |
| shader_find | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| shader_get_keywords | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| shader_get_properties | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| shader_get_variant_count | x | x | - | - | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py |
| shader_list | x | x | x | x | 2026-04-21: re-extracted from upstream 55b03ef3; 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; lambda-transform fix; 2026-04-21: Task 21 |
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
| smart_select_by_component | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-21: Task 21 |
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

## test (12 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| test_cancel | R | R | R | - | 2026-04-21: retired — TestRunnerApi has no public hard-cancel surface; file deleted |
| test_create_editmode | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: Task 5 async redesign |
| test_create_playmode | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: Task 5 async redesign |
| test_get_last_result | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: Task 5 async redesign; 2026-04-22: Task 21: live — TestResults/*.xml discovery works (1 report, 168KB); recipe XML-parsing logic was already comp:x under Task 5 |
| test_get_result | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: Task 5 async redesign; 2026-04-22: Task 21: live — TestResults/*.xml discovery works (1 report, 168KB); recipe XML-parsing logic was already comp:x under Task 5 |
| test_get_summary | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses undefined private helpers EnumerateRealTestRuns/GetResultInt/GetResultStringList from upstream TestSkills; 2026-04-21: Task 5 async redesign; 2026-04-22: Task 21: live — TestResults/*.xml discovery works (1 report, 168KB); recipe XML-parsing logic was already comp:x under Task 5 |
| test_list | x | x | x | x | 2026-04-22: Async redesign: fires TestRunnerApi.RetrieveTestList, callback writes Temp/test-list-<mode>.json. Paired with test_list_read + test_list_categories.; 2026-04-22: Task 21: live — test_list fired RetrieveTestList; callback wrote Temp/test-list-EditMode.json (42KB); reads parse cleanly |
| test_list_categories | x | x | x | x | 2026-04-22: Async redesign: stateless read of Temp/test-list-<mode>.json cache. Uses List<string>+manual Contains (ISet<> gotcha).; 2026-04-22: Task 21: live — test_list fired RetrieveTestList; callback wrote Temp/test-list-EditMode.json (42KB); reads parse cleanly |
| test_list_read | x | x | x | x | 2026-04-22: New recipe (Task 5 follow-up async split); stateless read of Temp/test-list-<mode>.json cache written by test_list.; 2026-04-22: Task 21: live — test_list fired RetrieveTestList; callback wrote Temp/test-list-EditMode.json (42KB); reads parse cleanly |
| test_run | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: Task 5 async redesign |
| test_run_by_name | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: Task 5 async redesign |
| test_smoke_skills | R | R | R | - | 2026-04-21: retired — depended on upstream REST SkillRegistry; file deleted |

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
| ui_create_batch | R | R | R | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Dispatcher pattern: delegates to 12 upstream UICreate* methods (canvas/panel/button/text/image/inputfield/slider/toggle/dropdown/scrollview/rawimage/scrollbar). Each primitive has ~50-100 lines of setup logic — full inlining would 10x the recipe size. Guidance: agents should call individual ui_create_canvas / ui_create_button / etc. recipes sequentially rather than this batch. No single-pattern rewrite fits.; 2026-04-22: Retired 2026-04-22: dispatcher to 12 UICreate* primitives, no unique logic. Agents call individual ui_create_<primitive> recipes sequentially. |
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
| uitk_create_batch | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: uses BatchExecutor / UIToolkitSkills helpers not in _shared; 2026-04-22: Task 15: BatchExecutor → typed _UitkFileItem foreach; File.WriteAllText + AssetDatabase.ImportAsset direct (UitkCreateUss/Uxml logic inlined minimally) |
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
| validate_shader_errors | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: comp smoke green; 2026-04-21: 6 errors across 298 shaders (Task 21) |
| validate_texture_sizes | x | x | - | - | 2026-04-21: scripted via inject_prerequisites.py |

## xr (22 recipes)

| recipe | ext | pre | comp | run | notes |
|---|---|---|---|---|---|
| xr_add_direct_interactor | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: XRDirectInteractor direct API |
| xr_add_grab_interactable | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: XRGrabInteractable + XRBaseInteractable.MovementType direct API |
| xr_add_interaction_event | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: UnityEventTools.AddVoidPersistentListener direct API |
| xr_add_ray_interactor | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: XRRayInteractor + XRInteractorLineVisual direct API |
| xr_add_simple_interactable | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: XRSimpleInteractable direct API |
| xr_add_socket_interactor | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: XRSocketInteractor direct API |
| xr_add_teleport_anchor | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: TeleportationAnchor + MatchOrientation direct API |
| xr_add_teleport_area | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: TeleportationArea direct API |
| xr_check_setup | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: direct XRI 3 types + FindFirstObjectByType migrations; 2026-04-22: Task 21: live — XR FindObjectsByType scans execute; current scene: 1 cam, 1 EventSystem, 0 XR infra |
| xr_configure_haptics | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: XRRayInteractor/XRDirectInteractor haptic props direct |
| xr_configure_interactable | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: XRBaseInteractable.selectMode + XRGrabInteractable props direct |
| xr_configure_interaction_layers | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: InteractionLayerMask.GetMask direct API |
| xr_get_scene_report | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: XRI 3 type enumeration + FindObjectsByType direct API; 2026-04-22: Task 21: live — XR FindObjectsByType scans execute; current scene: 1 cam, 1 EventSystem, 0 XR infra |
| xr_list_interactables | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: XRGrabInteractable/XRSimpleInteractable direct API; 2026-04-22: Task 21: live — XR FindObjectsByType scans execute; current scene: 1 cam, 1 EventSystem, 0 XR infra |
| xr_list_interactors | x | x | x | x | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: XRRayInteractor/XRDirectInteractor/XRSocketInteractor/NearFarInteractor direct API; 2026-04-22: Task 21: live — XR FindObjectsByType scans execute; current scene: 1 cam, 1 EventSystem, 0 XR infra |
| xr_setup_continuous_move | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: ContinuousMoveProvider direct API |
| xr_setup_event_system | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: XRI3 direct types, XRReflectionHelper removed (Task 14 pilot) |
| xr_setup_interaction_manager | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-21: #if !XRI gate + undefined NoXRI/XRReflectionHelper upstream helpers; references undeclared name param; 2026-04-22: Task 16: XRInteractionManager direct API (B cleared) |
| xr_setup_rig | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: XROrigin rig build direct API |
| xr_setup_teleportation | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: TeleportationProvider direct API |
| xr_setup_turn_provider | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: SnapTurnProvider/ContinuousTurnProvider direct API |
| xr_setup_ui_canvas | x | x | x | - | 2026-04-21: scripted via inject_prerequisites.py; 2026-04-22: Task 16: TrackedDeviceGraphicRaycaster direct API |

