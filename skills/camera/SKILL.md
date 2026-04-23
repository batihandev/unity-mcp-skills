---
name: unity-camera
description: "Use when users want to move, rotate, or align the editor camera view."
---

# Camera Skills

> Recipe path rule: `../../recipes/camera/<command>.md`
>
> Screenshots: `Unity_Camera_Capture` (native MCP) renders a camera in-memory by instance ID for quick inspection. `camera_screenshot` (this module) writes a PNG to a path under `Assets/` with custom resolution and looks the camera up by name / path / instanceId.

## Overview

Control the Scene View camera.

## Common Mistakes


**DO NOT** (common hallucinations):
- `camera_move` / `camera_rotate` do not exist → use `camera_set_transform` (Scene View) or `gameobject_set_transform` (Game Camera)
- `camera_set_fov` does not exist → use `camera_set_properties` with `fieldOfView` parameter
- `camera_*` skills control **two different cameras**: `camera_set_transform`/`camera_look_at`/`camera_align_view_to_object` control the **Scene View camera**; `camera_create`/`camera_set_properties`/`camera_screenshot` control **Game Cameras**
- `camera_delete` does not exist → use `gameobject_delete` on the camera GameObject

**Routing**:
- For Cinemachine virtual cameras → use `cinemachine` module
- For Game Camera component properties → `camera_set_properties` / `camera_get_properties` (this module)
- For scene screenshots → `scene_screenshot` (scene module) uses the Scene View; for in-memory Game Camera inspection use `Unity_Camera_Capture`; for persisted Game Camera PNGs at custom resolution use `camera_screenshot` (this module)

## Skills

### `camera_align_view_to_object`
Align Scene View camera to look at an object.
**Parameters:**
- `name` (string, optional): Target GameObject name.
- `instanceId` (int, optional): Target GameObject instance ID.
- `path` (string, optional): Target GameObject hierarchy path.

### `camera_get_info`
Get Scene View camera position and rotation.
**Parameters:** None.

### `camera_set_transform`
Set Scene View camera position/rotation manually.
**Parameters:**
- `posX`, `posY`, `posZ` (float): Position.
- `rotX`, `rotY`, `rotZ` (float): Rotation (Euler).
- `size` (float, optional): Orthographic size or distance.
- `instant` (bool, optional): Move instantly (default true).

### `camera_look_at`
Focus Scene View camera on a world-space point.
**Parameters:**
- `x`, `y`, `z` (float): Target point.
- Does not support `targetName` or GameObject lookup. For object focus, use `camera_align_view_to_object`.

### `camera_create`
Create a new Game Camera.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| name | string | No | "New Camera" | Name of the new camera GameObject |
| x | float | No | 0 | Position X |
| y | float | No | 1 | Position Y |
| z | float | No | -10 | Position Z |

**Returns:** `{ success, name, instanceId }`

### `camera_get_properties`
Get Game Camera properties (supports name/instanceId/path).

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| name | string | No | null | Name of the camera GameObject |
| instanceId | int | No | 0 | Instance ID of the camera GameObject |
| path | string | No | null | Hierarchy path of the camera GameObject |

**Returns:** `{ success, name, fieldOfView, nearClipPlane, farClipPlane, orthographic, orthographicSize, depth, cullingMask, clearFlags, backgroundColor, rect }`

### `camera_set_properties`
Set Game Camera properties (FOV, clip planes, clear flags, background color, depth).

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| name | string | No | null | Name of the camera GameObject |
| instanceId | int | No | 0 | Instance ID of the camera GameObject |
| path | string | No | null | Hierarchy path of the camera GameObject |
| fieldOfView | float? | No | null | Camera field of view |
| nearClipPlane | float? | No | null | Near clipping plane distance |
| farClipPlane | float? | No | null | Far clipping plane distance |
| depth | float? | No | null | Camera rendering depth |
| clearFlags | string | No | null | Clear flags (e.g. Skybox, SolidColor, Depth, Nothing) |
| bgR | float? | No | null | Background color red component |
| bgG | float? | No | null | Background color green component |
| bgB | float? | No | null | Background color blue component |

**Returns:** `{ success, name }`

### `camera_set_culling_mask`
Set Game Camera culling mask by layer names (comma-separated).

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| layerNames | string | Yes | - | Comma-separated layer names |
| name | string | No | null | Name of the camera GameObject |
| instanceId | int | No | 0 | Instance ID of the camera GameObject |
| path | string | No | null | Hierarchy path of the camera GameObject |

**Returns:** `{ success, cullingMask }`

### `camera_screenshot`
Capture a PNG from a Game Camera to a path under `Assets/` at a custom resolution. Writes the file and calls `AssetDatabase.ImportAsset` so it shows up as a project asset. For a quick in-memory capture without disk persistence, use the native `Unity_Camera_Capture` tool instead.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| savePath | string | No | "Assets/screenshot.png" | PNG output path under `Assets/` |
| width | int | No | 1920 | Render width in pixels |
| height | int | No | 1080 | Render height in pixels |
| name | string | No | null | Game Camera GameObject name |
| instanceId | int | No | 0 | Game Camera GameObject instance ID |
| path | string | No | null | Game Camera hierarchy path |

**Returns:** `{ success, path, width, height }`

### `camera_set_orthographic`
Switch Game Camera between orthographic and perspective mode.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| orthographic | bool | Yes | - | True for orthographic, false for perspective |
| orthographicSize | float? | No | null | Orthographic size (only applies in orthographic mode) |
| name | string | No | null | Name of the camera GameObject |
| instanceId | int | No | 0 | Instance ID of the camera GameObject |
| path | string | No | null | Hierarchy path of the camera GameObject |

**Returns:** `{ success, orthographic, orthographicSize }`

### `camera_list`
List all cameras in the scene.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|

**Returns:** `{ count, cameras: [{ name, instanceId, path, depth, orthographic, enabled }] }`
