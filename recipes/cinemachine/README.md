# Cinemachine Recipes

Per-command recipes for the `unity-cinemachine` skill. Each file corresponds to one command ID.

## Commands

| File | Command | Description |
|------|---------|-------------|
| [cinemachine_create_vcam.md](cinemachine_create_vcam.md) | `cinemachine_create_vcam` | Create a new Virtual Camera |
| [cinemachine_inspect_vcam.md](cinemachine_inspect_vcam.md) | `cinemachine_inspect_vcam` | Deeply inspect a VCam |
| [cinemachine_set_vcam_property.md](cinemachine_set_vcam_property.md) | `cinemachine_set_vcam_property` | Set any property on VCam or pipeline component |
| [cinemachine_set_targets.md](cinemachine_set_targets.md) | `cinemachine_set_targets` | Set Follow and LookAt targets |
| [cinemachine_set_component.md](cinemachine_set_component.md) | `cinemachine_set_component` | Switch pipeline component (Body/Aim/Noise) — CM3 only |
| [cinemachine_add_component.md](cinemachine_add_component.md) | `cinemachine_add_component` | Add component (legacy/CM2) |
| [cinemachine_set_lens.md](cinemachine_set_lens.md) | `cinemachine_set_lens` | Configure Lens settings (FOV, clip planes, ortho size) |
| [cinemachine_list_components.md](cinemachine_list_components.md) | `cinemachine_list_components` | List available Cinemachine component names |
| [cinemachine_impulse_generate.md](cinemachine_impulse_generate.md) | `cinemachine_impulse_generate` | Trigger an Impulse |
| [cinemachine_get_brain_info.md](cinemachine_get_brain_info.md) | `cinemachine_get_brain_info` | Get active camera and blend info from Brain |
| [cinemachine_create_target_group.md](cinemachine_create_target_group.md) | `cinemachine_create_target_group` | Create a CinemachineTargetGroup |
| [cinemachine_target_group_add_member.md](cinemachine_target_group_add_member.md) | `cinemachine_target_group_add_member` | Add/update member in TargetGroup |
| [cinemachine_target_group_remove_member.md](cinemachine_target_group_remove_member.md) | `cinemachine_target_group_remove_member` | Remove member from TargetGroup |
| [cinemachine_set_spline.md](cinemachine_set_spline.md) | `cinemachine_set_spline` | Assign SplineContainer to SplineDolly — CM3 + Splines only |
| [cinemachine_add_extension.md](cinemachine_add_extension.md) | `cinemachine_add_extension` | Add a CinemachineExtension to VCam |
| [cinemachine_remove_extension.md](cinemachine_remove_extension.md) | `cinemachine_remove_extension` | Remove a CinemachineExtension from VCam |
| [cinemachine_set_active.md](cinemachine_set_active.md) | `cinemachine_set_active` | Force VCam active (SOLO) by setting highest priority |
| [cinemachine_create_mixing_camera.md](cinemachine_create_mixing_camera.md) | `cinemachine_create_mixing_camera` | Create a Mixing Camera |
| [cinemachine_mixing_camera_set_weight.md](cinemachine_mixing_camera_set_weight.md) | `cinemachine_mixing_camera_set_weight` | Set child camera weight in Mixing Camera |
| [cinemachine_create_clear_shot.md](cinemachine_create_clear_shot.md) | `cinemachine_create_clear_shot` | Create a ClearShot camera |
| [cinemachine_create_state_driven_camera.md](cinemachine_create_state_driven_camera.md) | `cinemachine_create_state_driven_camera` | Create a StateDriven camera |
| [cinemachine_state_driven_camera_add_instruction.md](cinemachine_state_driven_camera_add_instruction.md) | `cinemachine_state_driven_camera_add_instruction` | Add state-to-camera instruction |
| [cinemachine_set_noise.md](cinemachine_set_noise.md) | `cinemachine_set_noise` | Configure Noise (Basic Multi Channel Perlin) |
| [cinemachine_set_priority.md](cinemachine_set_priority.md) | `cinemachine_set_priority` | Set explicit priority value on VCam |
| [cinemachine_set_blend.md](cinemachine_set_blend.md) | `cinemachine_set_blend` | Set default blend on CinemachineBrain |
| [cinemachine_set_brain.md](cinemachine_set_brain.md) | `cinemachine_set_brain` | Configure CinemachineBrain properties |
| [cinemachine_create_sequencer.md](cinemachine_create_sequencer.md) | `cinemachine_create_sequencer` | Create Sequencer (CM3) or BlendList (CM2) camera |
| [cinemachine_sequencer_add_instruction.md](cinemachine_sequencer_add_instruction.md) | `cinemachine_sequencer_add_instruction` | Add child camera instruction to Sequencer |
| [cinemachine_create_freelook.md](cinemachine_create_freelook.md) | `cinemachine_create_freelook` | Create a FreeLook camera |
| [cinemachine_configure_camera_manager.md](cinemachine_configure_camera_manager.md) | `cinemachine_configure_camera_manager` | Configure ClearShot/StateDriven/Sequencer properties |
| [cinemachine_configure_body.md](cinemachine_configure_body.md) | `cinemachine_configure_body` | Configure Body stage component |
| [cinemachine_configure_aim.md](cinemachine_configure_aim.md) | `cinemachine_configure_aim` | Configure Aim stage component |
| [cinemachine_configure_extension.md](cinemachine_configure_extension.md) | `cinemachine_configure_extension` | Configure a Cinemachine extension |
| [cinemachine_configure_impulse_source.md](cinemachine_configure_impulse_source.md) | `cinemachine_configure_impulse_source` | Configure CinemachineImpulseSource definition |

## Compatibility

All recipes support both Cinemachine 2.x and 3.x unless noted otherwise:
- CM2 uses `Cinemachine` namespace; CM3 uses `Unity.Cinemachine`
- `cinemachine_set_component` is CM3 only (use `cinemachine_add_component` for CM2)
- `cinemachine_set_spline` requires CM3 + `com.unity.splines`

## Usage

Use these templates in `Unity_RunCommand`. Recipe path rule: `../../recipes/cinemachine/<command>.md`
