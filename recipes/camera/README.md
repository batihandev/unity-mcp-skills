# Camera Recipes

Per-command recipes for the `unity-camera` skill. Each file corresponds to one command ID.

## Commands

| File | Command |
|------|---------|
| [camera_align_view_to_object.md](camera_align_view_to_object.md) | `camera_align_view_to_object` |
| [camera_get_info.md](camera_get_info.md) | `camera_get_info` |
| [camera_set_transform.md](camera_set_transform.md) | `camera_set_transform` |
| [camera_look_at.md](camera_look_at.md) | `camera_look_at` |
| [camera_create.md](camera_create.md) | `camera_create` |
| [camera_get_properties.md](camera_get_properties.md) | `camera_get_properties` |
| [camera_set_properties.md](camera_set_properties.md) | `camera_set_properties` |
| [camera_set_culling_mask.md](camera_set_culling_mask.md) | `camera_set_culling_mask` |
| [camera_screenshot.md](camera_screenshot.md) | `camera_screenshot` |
| [camera_set_orthographic.md](camera_set_orthographic.md) | `camera_set_orthographic` |
| [camera_list.md](camera_list.md) | `camera_list` |

## Native Tool Overlap

`camera_screenshot` overlaps with the native `Unity_Camera_Capture` tool. Prefer the native tool for simple captures; use `camera_screenshot` when you need precise resolution control or a specific camera target.

## Usage

Use these templates in `Unity_RunCommand`. Recipe path rule: `../../recipes/camera/<command>.md`
