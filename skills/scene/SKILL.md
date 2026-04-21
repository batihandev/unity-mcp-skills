---
name: unity-scene
description: "Use when users want to create, load, save scenes, or get scene hierarchy."
---

# Unity Scene Skills

## Overview

Control Unity scenes - the containers that hold all your GameObjects.

## Common Mistakes


**DO NOT** (common hallucinations):
- `scene_delete` / `scene_rename` do not exist → delete scene files via `asset_delete`, rename via `asset_move`
- `scene_list` does not exist → use `scene_get_loaded` (loaded scenes) or `asset_find` with `t:Scene` (all scene assets)
- `scene_find_objects` is a simple name/tag/component filter; for regex/layer/path search use `gameobject_find` (Full-Auto)

**Routing**:
- For detailed hierarchy tree → use `perception` module's `hierarchy_describe`
- For scene statistics → use `perception` module's `scene_summarize`
- For screenshot → `scene_screenshot` (this module) or `camera_screenshot` (camera module, Full-Auto)

## Skills Overview

| Skill | Description |
|-------|-------------|
| `scene_create` | Create a new scene |
| `scene_load` | Load a scene |
| `scene_save` | Save current scene |
| `scene_get_info` | Get scene information |
| `scene_get_hierarchy` | Get hierarchy tree |
| `scene_screenshot` | Capture screenshot |
| `scene_get_loaded` | Get all loaded scenes |
| `scene_unload` | Unload an additive scene |
| `scene_set_active` | Set active scene |
| `scene_find_objects` | Search objects by name/tag/component |

---

## Skills

### scene_create
Create a new scene.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `scenePath` | string | Yes | Path for new scene (e.g., "Assets/Scenes/MyScene.unity") |

### scene_load
Load a scene.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `scenePath` | string | Yes | - | Scene asset path |
| `additive` | bool | No | false | Load additively (keep current scene) |

### scene_save
Save the current scene.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `scenePath` | string | No | Save path (null = save current) |

### scene_get_info
Get current scene information.

No parameters.

**Returns**: `{success, name, path, isDirty, rootObjectCount, rootObjects: [name]}`

### scene_get_hierarchy
Get full scene hierarchy tree.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `maxDepth` | int | No | 10 | Maximum hierarchy depth |

**Returns**: `{success, hierarchy: [{name, instanceId, children: [...]}]}`

### scene_screenshot
Capture a screenshot.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `filename` | string | No | "screenshot.png" | Output filename |
| `width` | int | No | 1920 | Image width |
| `height` | int | No | 1080 | Image height |

### scene_get_loaded
Get list of all currently loaded scenes.

No parameters.

**Returns**: `{success, scenes: [{name, path, isActive, isDirty}]}`

### scene_unload
Unload a loaded scene (additive).

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `sceneName` | string | Yes | Scene name to unload |

### scene_set_active
Set the active scene (for multi-scene editing).

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `sceneName` | string | Yes | Scene name to set active |

### scene_find_objects
Search GameObjects by name pattern, tag, or component type. For advanced search (regex, layer, path) use gameobject_find.

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `namePattern` | string | No | - | Name substring to match (case-insensitive) |
| `tag` | string | No | - | Filter by tag |
| `componentType` | string | No | - | Filter by component type name |
| `limit` | int | No | 50 | Max results to return |

**Returns**: `{success, count, objects: [{name, path, instanceId, active, tag}]}`

---

## Example Usage

*See `../../recipes/scene/<command>.md` for C# templates.*

Recipe path rule: `../../recipes/scene/<command>.md`

## Best Practices

1. Always save before loading a new scene
2. Use additive loading for UI overlays
3. Keep scene hierarchy organized with empty parent objects
4. Use `scene_get_info` to verify scene state
5. Screenshots are saved to project root by default

